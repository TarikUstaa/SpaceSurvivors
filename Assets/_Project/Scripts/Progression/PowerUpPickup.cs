using SpaceSurvivors.Core;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// A power-up dropped by enemies. Flies to the ship and applies a timed
    /// <see cref="StatModifier"/> via <see cref="PlayerPowerUps"/> (default: faster fire
    /// rate), with a one-shot burst VFX. Author the stat / amount / duration on the prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public class PowerUpPickup : FlyToPlayerPickup
    {
        [Header("Effect")]
        [Tooltip("The timed stat change granted on pickup.")]
        [SerializeField]
        private StatModifier _modifier = new(StatId.FireRate, ModifierOp.PercentAdd, 0.4f);
        [SerializeField, Min(0.5f)] private float _duration = 8f;
        [Tooltip("Buffs sharing a label refresh the timer instead of stacking.")]
        [SerializeField] private string _label = "Overdrive";
        [Tooltip("Pooled OneShotPulse played on the ship when collected.")]
        [SerializeField] private GameObject _collectVfxPrefab;

        private PoolManager _pool;

        public override void OnSpawned()
        {
            base.OnSpawned();
            if (_pool == null) _pool = FindAnyObjectByType<PoolManager>();
        }

        protected override void OnCollected(GameObject collector)
        {
            if (collector.TryGetComponent(out PlayerPowerUps powerUps))
                powerUps.Apply(_modifier, _duration, _label);

            if (_collectVfxPrefab != null && _pool != null)
                _pool.Spawn(_collectVfxPrefab, collector.transform.position, Quaternion.identity);
        }
    }
}
