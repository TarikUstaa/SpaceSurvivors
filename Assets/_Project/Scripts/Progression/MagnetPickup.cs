using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// A magnet dropped by enemies. On pickup, every scrap/XP drop currently on the map
    /// flies straight to the ship (<see cref="XpPickup.Attract"/>) — a screen-clear grab.
    /// </summary>
    [DisallowMultipleComponent]
    public class MagnetPickup : FlyToPlayerPickup
    {
        [Header("Effect")]
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
            foreach (var drop in FindObjectsByType<XpPickup>())
                drop.Attract();

            if (_collectVfxPrefab != null && _pool != null)
                _pool.Spawn(_collectVfxPrefab, collector.transform.position, Quaternion.identity);
        }
    }
}
