using System;
using System.Collections.Generic;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// Holds the player's timed power-up buffs. A <see cref="PowerUpPickup"/> calls
    /// <see cref="Apply"/>; this component pushes the <see cref="StatModifier"/> onto the
    /// <see cref="StatSheet"/>, counts down, and pulls it back off when it expires — so the
    /// rest of the game sees a power-up as an ordinary (temporary) stat change
    /// (AI_Guidelines §1, §3). Also drives the ship's buff aura.
    /// </summary>
    [RequireComponent(typeof(StatSheet))]
    [DisallowMultipleComponent]
    public class PlayerPowerUps : MonoBehaviour
    {
        [SerializeField] private StatSheet _stats;

        [Header("Ship aura (optional)")]
        [Tooltip("Child object shown while any power-up is active.")]
        [SerializeField] private Transform _aura;
        [SerializeField] private float _auraSpinSpeed = 110f;
        [SerializeField] private float _auraPulseSpeed = 5f;
        [SerializeField, Range(0f, 0.5f)] private float _auraPulseAmount = 0.09f;

        private sealed class ActiveBuff
        {
            public StatModifier Modifier;
            public string Label;
            public float EndTime;
        }

        private readonly List<ActiveBuff> _active = new();
        private Vector3 _auraBaseScale = Vector3.one;

        /// <summary>Raised whenever a buff is applied, refreshed, or expires (for the HUD).</summary>
        public event Action Changed;

        /// <summary>Seconds left on the longest-running buff (0 = none). For a HUD readout.</summary>
        public float LongestRemaining
        {
            get
            {
                float best = 0f;
                for (int i = 0; i < _active.Count; i++)
                    best = Mathf.Max(best, _active[i].EndTime - Time.time);
                return Mathf.Max(0f, best);
            }
        }

        public bool AnyActive => _active.Count > 0;

        private void Awake()
        {
            if (_stats == null) _stats = GetComponent<StatSheet>();
            if (_aura != null)
            {
                _auraBaseScale = _aura.localScale;
                _aura.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Apply (or refresh) a timed buff. Re-collecting the same <paramref name="label"/>
        /// extends the timer instead of stacking the modifier.
        /// </summary>
        public void Apply(StatModifier modifier, float duration, string label)
        {
            var existing = _active.Find(a => a.Label == label);
            if (existing != null)
            {
                existing.EndTime = Mathf.Max(existing.EndTime, Time.time + duration);
            }
            else
            {
                _stats.AddModifier(modifier);
                _active.Add(new ActiveBuff { Modifier = modifier, Label = label, EndTime = Time.time + duration });
            }

            if (_aura != null && !_aura.gameObject.activeSelf)
                _aura.gameObject.SetActive(true);

            Changed?.Invoke();
        }

        private void Update()
        {
            if (_active.Count > 0)
            {
                bool changed = false;
                for (int i = _active.Count - 1; i >= 0; i--)
                {
                    if (Time.time >= _active[i].EndTime)
                    {
                        _stats.RemoveModifier(_active[i].Modifier);
                        _active.RemoveAt(i);
                        changed = true;
                    }
                }
                if (changed)
                {
                    if (_active.Count == 0 && _aura != null) _aura.gameObject.SetActive(false);
                    Changed?.Invoke();
                }
            }

            if (_aura != null && _aura.gameObject.activeSelf)
            {
                _aura.Rotate(0f, 0f, _auraSpinSpeed * Time.deltaTime);
                float pulse = 1f + Mathf.Sin(Time.time * _auraPulseSpeed) * _auraPulseAmount;
                _aura.localScale = _auraBaseScale * pulse;
            }
        }
    }
}
