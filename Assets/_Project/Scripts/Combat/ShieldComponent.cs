using System;
using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// A stackable, self-recharging shield for the player.
    ///
    /// * <see cref="IDamageInterceptor"/> — while it has a charge, it fully <b>absorbs</b>
    ///   the next incoming damage instance, spends one charge, and <b>reflects</b> the hit
    ///   back to whatever caused it.
    /// * While up it is also a <b>barrier</b>: enemies touching the ship's collider take
    ///   periodic damage (they can't just sit on you for free).
    /// * Charges come back one at a time, <see cref="_rechargeTime"/> seconds after the last
    ///   loss. Max charges = base + <see cref="StatId.ShieldCharges"/> from the StatSheet, so
    ///   the "Shield" upgrade both unlocks it and stacks it (AI_Guidelines §3).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class ShieldComponent : MonoBehaviour, IDamageInterceptor
    {
        [Header("Charges")]
        [Tooltip("Charges before any upgrade. 0 = the Shield upgrade is what unlocks it.")]
        [SerializeField, Min(0)] private int _baseCharges = 0;
        [SerializeField] private StatSheet _stats;

        [Header("Recharge")]
        [Tooltip("Seconds after losing a charge before one regenerates.")]
        [SerializeField, Min(0.1f)] private float _rechargeTime = 6f;

        [Header("Reflect")]
        [Tooltip("Bonus damage added on top of the absorbed hit when reflecting to the attacker.")]
        [SerializeField, Min(0f)] private float _reflectBonus = 12f;

        [Tooltip("Invulnerability granted right after the shield absorbs a hit, so a broken " +
                 "shield doesn't mean instantly eating the next contact tick.")]
        [SerializeField, Min(0f)] private float _graceAfterAbsorb = 1f;

        [Header("Barrier (while shield is up)")]
        [SerializeField] private LayerMask _enemyLayers;
        [SerializeField, Min(0f)] private float _barrierDamage = 6f;
        [SerializeField, Min(0.05f)] private float _barrierInterval = 0.35f;

        public int CurrentCharges { get; private set; }
        public int MaxCharges { get; private set; }
        public bool IsUp => CurrentCharges > 0;

        /// <summary>(current, max) — for the HUD and the shield bubble view.</summary>
        public event Action<int, int> ShieldChanged;
        /// <summary>Fired the moment a charge absorbs a hit.</summary>
        public event Action Absorbed;

        private float _rechargeTimer;
        private HealthComponent _health;
        private readonly Dictionary<Collider2D, float> _barrierCooldowns = new();

        private void Awake()
        {
            if (_stats == null) _stats = GetComponent<StatSheet>();
            _health = GetComponent<HealthComponent>();
            RecalcMax(fill: true);
        }

        private void OnEnable()
        {
            if (_stats != null) _stats.Changed += HandleStatsChanged;
        }

        private void OnDisable()
        {
            if (_stats != null) _stats.Changed -= HandleStatsChanged;
        }

        private void HandleStatsChanged() => RecalcMax(fill: false);

        private void RecalcMax(bool fill)
        {
            int newMax = _stats != null
                ? Mathf.RoundToInt(_stats.Modify(StatId.ShieldCharges, _baseCharges))
                : _baseCharges;
            newMax = Mathf.Max(0, newMax);

            int gained = newMax - MaxCharges;
            MaxCharges = newMax;

            if (fill) CurrentCharges = MaxCharges;
            else if (gained > 0) CurrentCharges = Mathf.Min(MaxCharges, CurrentCharges + gained); // a new stack arrives charged
            else CurrentCharges = Mathf.Min(CurrentCharges, MaxCharges);

            RaiseChanged();
        }

        // ---- IDamageInterceptor ----
        public bool Intercept(in DamageInfo info)
        {
            if (CurrentCharges <= 0) return false;

            CurrentCharges--;
            _rechargeTimer = _rechargeTime;

            // Breathing room: block all damage briefly so a broken shield isn't an instant hit.
            if (_graceAfterAbsorb > 0f && _health != null)
                _health.GrantInvulnerability(_graceAfterAbsorb);

            RaiseChanged();
            Absorbed?.Invoke();

            ReflectTo(info.Source, info.Amount + _reflectBonus, info.HitPoint);
            return true;
        }

        private void Update()
        {
            if (CurrentCharges >= MaxCharges) return;

            _rechargeTimer -= Time.deltaTime;
            if (_rechargeTimer <= 0f)
            {
                CurrentCharges++;
                _rechargeTimer = _rechargeTime;
                RaiseChanged();
            }
        }

        // ---- Barrier ----
        private void OnCollisionStay2D(Collision2D collision) => TryBarrier(collision.collider);
        private void OnCollisionEnter2D(Collision2D collision) => TryBarrier(collision.collider);
        private void OnCollisionExit2D(Collision2D collision) => _barrierCooldowns.Remove(collision.collider);

        private void TryBarrier(Collider2D other)
        {
            if (CurrentCharges <= 0 || other == null) return;
            if ((_enemyLayers.value & (1 << other.gameObject.layer)) == 0) return;

            float now = Time.time;
            if (_barrierCooldowns.TryGetValue(other, out float next) && now < next) return;
            _barrierCooldowns[other] = now + _barrierInterval;

            ReflectTo(other.gameObject, _barrierDamage, other.transform.position);
        }

        private void ReflectTo(GameObject attacker, float amount, Vector2 point)
        {
            if (attacker == null || amount <= 0f) return;
            var target = attacker.GetComponentInParent<IDamageable>();
            if (target != null && target.IsAlive)
                target.TakeDamage(new DamageInfo(amount, gameObject, point, (Vector2)attacker.transform.position - point));
        }

        private void RaiseChanged() => ShieldChanged?.Invoke(CurrentCharges, MaxCharges);
    }
}
