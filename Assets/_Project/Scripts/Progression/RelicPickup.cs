using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// A relic sitting in the world. Flies to the ship like any other bonus pickup
    /// (<see cref="FlyToPlayerPickup"/>) and, on collect, hands itself to the collector's
    /// <see cref="RelicService"/> rather than a timed-buff system like
    /// <see cref="PlayerPowerUps"/> — a relic is permanent, not a duration.
    /// </summary>
    [DisallowMultipleComponent]
    public class RelicPickup : FlyToPlayerPickup
    {
        [Tooltip("Which relic this pickup grants. Set by the dropper, or authored on a fixed prefab.")]
        [SerializeField] private Data.RelicData _relic;

        [Tooltip("Pooled OneShotPulse played on the ship when collected.")]
        [SerializeField] private GameObject _collectVfxPrefab;

        private PoolManager _pool;

        public override void OnSpawned()
        {
            base.OnSpawned();
            if (_pool == null) _pool = FindAnyObjectByType<PoolManager>();
        }

        /// <summary>Set which relic this spawned instance grants — the dropper picks one per drop.</summary>
        public void SetRelic(Data.RelicData relic) => _relic = relic;

        protected override void OnCollected(GameObject collector)
        {
            if (_relic != null && collector.TryGetComponent(out RelicService relics))
                relics.TryGrant(_relic);

            if (_collectVfxPrefab != null && _pool != null)
                _pool.Spawn(_collectVfxPrefab, collector.transform.position, Quaternion.identity);
        }
    }
}
