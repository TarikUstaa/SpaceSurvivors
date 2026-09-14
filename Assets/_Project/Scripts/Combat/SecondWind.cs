using System;
using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// A once-per-run safety net: the hit that would have ended the run instead gets fully
    /// absorbed, the ship heals to a fraction of max HP, and a moment of invulnerability
    /// covers the recovery. Composes exactly like <see cref="ShieldComponent"/> — same
    /// <see cref="IDamageInterceptor"/> interface, and reuses <see cref="HealthComponent"/>'s
    /// own <see cref="HealthComponent.Heal"/> / <see cref="HealthComponent.GrantInvulnerability"/>
    /// rather than touching HP directly, so nothing about death/run-end had to change
    /// (AI_Guidelines §1, §2) — the interceptor never lets <see cref="HealthComponent"/> see a
    /// lethal hit as lethal in the first place.
    ///
    /// <para>Checked against the incoming hit's raw amount, before armour resist is applied —
    /// slightly generous (a hit resist would have reduced below-lethal can still trigger a
    /// revive), which is the right direction to be wrong in for a last-resort safety net.</para>
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    [DisallowMultipleComponent]
    public class SecondWind : MonoBehaviour, IDamageInterceptor
    {
        [Tooltip("Revives available per run. 1 = one free save from death.")]
        [SerializeField, Min(0)] private int _chargesPerRun = 1;
        [Tooltip("Fraction of max HP restored on revive.")]
        [SerializeField, Range(0.05f, 1f)] private float _reviveHealthFraction = 0.3f;
        [Tooltip("Seconds of invulnerability granted right after reviving.")]
        [SerializeField, Min(0f)] private float _graceInvulnerability = 2f;

        private HealthComponent _health;

        public int ChargesRemaining { get; private set; }

        /// <summary>Raised the moment a charge saves the run — a toast/VFX can hook this later
        /// without anything here needing to change.</summary>
        public event Action Revived;

        private void Awake() => _health = GetComponent<HealthComponent>();

        // A fresh run means a fresh Player instance (the scene reloads on Replay), so charges
        // resetting here — rather than being tracked externally per-run — is enough; nothing
        // needs to explicitly "start a new run" for this component to know about it.
        private void OnEnable() => ChargesRemaining = _chargesPerRun;

        /// <inheritdoc />
        public bool Intercept(in DamageInfo info)
        {
            if (ChargesRemaining <= 0 || _health == null) return false;
            if (info.Amount < _health.Current) return false; // not lethal — let HealthComponent handle it normally

            ChargesRemaining--;
            _health.Heal(_health.Max * _reviveHealthFraction);
            _health.GrantInvulnerability(_graceInvulnerability);
            Revived?.Invoke();
            return true;
        }
    }
}
