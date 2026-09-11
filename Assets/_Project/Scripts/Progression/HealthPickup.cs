using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// A health capsule dropped by enemies. Flies to the ship and restores
    /// <see cref="_healAmount"/> HP (capped at max), with a one-shot burst VFX.
    /// </summary>
    [DisallowMultipleComponent]
    public class HealthPickup : FlyToPlayerPickup
    {
        [Header("Effect")]
        [SerializeField, Min(1f)] private float _healAmount = 25f;
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
            if (collector.TryGetComponent(out HealthComponent health))
                health.Heal(_healAmount);

            if (_collectVfxPrefab != null && _pool != null)
                _pool.Spawn(_collectVfxPrefab, collector.transform.position, Quaternion.identity);
        }
    }
}
