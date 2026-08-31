using System;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using UnityEngine;
using UnityEngine.Events;

namespace SpaceSurvivors.Combat
{
    /// <summary>Payload for <see cref="HealthComponent.HealthChanged"/>.</summary>
    public readonly struct HealthChangedArgs
    {
        public readonly float Current;
        public readonly float Max;
        /// <summary>Signed change that just occurred (negative = damage, positive = heal).</summary>
        public readonly float Delta;

        public HealthChangedArgs(float current, float max, float delta)
        {
            Current = current;
            Max = max;
            Delta = delta;
        }

        public float Normalized => Max > 0f ? Current / Max : 0f;
    }

    /// <summary>
    /// Re-usable hit-points component for the player AND every enemy (AI_Guidelines §1 —
    /// one job, shared by composition, never a bespoke "PlayerHealth" + "EnemyHealth").
    ///
    /// * Implements <see cref="IDamageable"/> so attackers stay decoupled (§2).
    /// * All numbers come from a <see cref="HealthData"/> asset (§3).
    /// * Math is delegated to the pure, testable <see cref="HealthState"/> (§7).
    /// * Pool-friendly: <see cref="OnEnable"/> restores full HP so recycled objects are
    ///   fresh (§4). No Instantiate/Destroy here.
    ///
    /// Other systems react through the C# events (code) or the UnityEvents (designer
    /// wiring in the Inspector) — this component never calls into them directly.
    /// </summary>
    [DisallowMultipleComponent]
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [Header("Data")]
        [SerializeField] private HealthData _data;

        [Tooltip("Blocks all incoming damage while true (debug, cutscenes, spawn grace).")]
        [SerializeField] private bool _invulnerable;

        [Tooltip("Optional (player only). If set, max HP runs through StatId.MaxHealth.")]
        [SerializeField] private SpaceSurvivors.Stats.StatSheet _stats;

        [Header("Designer hooks (optional)")]
        [SerializeField] private UnityEvent _onDamaged;
        [SerializeField] private UnityEvent _onHealed;
        [SerializeField] private UnityEvent _onDied;

        // Code-facing events with full payloads.
        public event Action<HealthChangedArgs> HealthChanged;
        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;

        private HealthState _state;
        private float _iFrameTimer;
        private IDamageInterceptor[] _interceptors;

        public bool IsAlive => _state != null && _state.IsAlive;
        public float Current => _state?.Current ?? 0f;
        public float Max => _state?.Max ?? 0f;
        public float Normalized => _state?.Normalized ?? 0f;

        public bool Invulnerable
        {
            get => _invulnerable;
            set => _invulnerable = value;
        }

        private void Awake()
        {
            float baseMax = _data != null ? _data.maxHealth : 1f;
            float max = _stats != null
                ? _stats.Modify(SpaceSurvivors.Stats.StatId.MaxHealth, baseMax)
                : baseMax;
            _state = new HealthState(max);
            _interceptors = GetComponents<IDamageInterceptor>();

            if (_data == null)
                Debug.LogError($"{nameof(HealthComponent)} on '{name}' has no HealthData assigned.", this);
        }

        private void ApplyStatMaxHealth()
        {
            if (_data == null || _state == null) return;
            float missing = _state.Max - _state.Current;
            float newMax = _stats.Modify(SpaceSurvivors.Stats.StatId.MaxHealth, _data.maxHealth);
            _state.SetMax(newMax, refillToFull: false);
            // Preserve the "missing HP" amount, so +MaxHealth also heals by that much now.
            _state.Apply((newMax - missing) - _state.Current);
            RaiseHealthChanged(0f);
        }

        // Pool lifecycle: a recycled object comes back at full health with no i-frames.
        private void OnEnable()
        {
            _state?.ResetToFull();
            _iFrameTimer = 0f;
            if (_stats != null) _stats.Changed += ApplyStatMaxHealth;
            RaiseHealthChanged(0f);
        }

        private void OnDisable()
        {
            if (_stats != null) _stats.Changed -= ApplyStatMaxHealth;
        }

        private void Update()
        {
            if (_iFrameTimer > 0f)
                _iFrameTimer -= Time.deltaTime;
        }

        /// <inheritdoc />
        public bool TakeDamage(in DamageInfo info)
        {
            // Returns false when the hit doesn't land (dead / i-framed / invulnerable / no
            // damage). Attackers use this to avoid wasting a projectile on a target another
            // shot already killed in the same frame.
            if (!IsAlive || _invulnerable || _iFrameTimer > 0f) return false;
            if (info.Amount <= 0f) return false;

            // Shields / parries get first look and may swallow the hit entirely.
            for (int i = 0; i < _interceptors.Length; i++)
            {
                if (_interceptors[i].Intercept(info))
                    return true; // the hit "landed" (attacker did its thing) but no HP lost
            }

            // Armour: a fraction of the hit is ignored (player only — enemies have no _stats).
            float amount = info.Amount;
            if (_stats != null)
            {
                float resist = Mathf.Clamp(
                    _stats.Modify(SpaceSurvivors.Stats.StatId.DamageResist, 0f), 0f, 0.85f);
                amount *= 1f - resist;
            }

            float applied = _state.Apply(-amount); // negative delta

            if (_data != null && _data.invulnerabilityAfterHit > 0f)
                _iFrameTimer = _data.invulnerabilityAfterHit;

            RaiseHealthChanged(applied);
            _onDamaged?.Invoke();
            Damaged?.Invoke(info);

            if (!_state.IsAlive)
            {
                _onDied?.Invoke();
                Died?.Invoke(info);
            }

            return true;
        }

        /// <summary>
        /// Grant a window of invulnerability (i-frames). Extends but never shortens the
        /// current window. Used by the shield after it absorbs a hit so the player gets a
        /// beat to reposition instead of eating the next contact tick.
        /// </summary>
        public void GrantInvulnerability(float seconds)
        {
            if (seconds > _iFrameTimer) _iFrameTimer = seconds;
        }

        /// <summary>Restore hit points (pickups, upgrades). Ignored if already dead.</summary>
        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            float applied = _state.Apply(amount);
            if (applied <= 0f) return;

            RaiseHealthChanged(applied);
            _onHealed?.Invoke();
        }

        /// <summary>Raise the HP ceiling at runtime (e.g. a "+20 max HP" upgrade).</summary>
        public void SetMaxHealth(float newMax, bool healToFull)
        {
            _state.SetMax(newMax, healToFull);
            RaiseHealthChanged(0f);
        }

        /// <summary>Force-restore to full (spawn director / pool reuse from code).</summary>
        public void ResetToFull()
        {
            _state.ResetToFull();
            _iFrameTimer = 0f;
            RaiseHealthChanged(0f);
        }

        private void RaiseHealthChanged(float delta)
        {
            HealthChanged?.Invoke(new HealthChangedArgs(Current, Max, delta));
        }
    }
}
