using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Enemies;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// Listens to <see cref="SpawnDirector.EnemyKilled"/> and drops a pooled
    /// <see cref="RelicPickup"/>: guaranteed on every boss kill (detected via
    /// <see cref="BossMarker"/> on the killed enemy), and a small independent chance on any
    /// regular kill. Picks a random relic the run doesn't already own via
    /// <see cref="RelicService"/>, so a repeat drop never wastes itself on a duplicate.
    /// Parallel to <see cref="LootDropper"/> on purpose — same event, separate component,
    /// so relics stay their own concern (AI_Guidelines §1, §4).
    /// </summary>
    [DisallowMultipleComponent]
    public class RelicDropper : MonoBehaviour
    {
        private const string CatalogueResource = "RelicCatalogue";

        [SerializeField] private PoolManager _pool;
        [SerializeField] private SpawnDirector _spawnDirector;
        [SerializeField] private ScrapCollector _collector;
        [SerializeField] private RelicService _relicService;
        [SerializeField] private GameObject _relicPickupPrefab;

        [Tooltip("Chance a non-boss kill drops a relic. Boss kills always drop one (if any relic is left unowned).")]
        [SerializeField, Range(0f, 1f)] private float _regularDropChance = 0.005f;

        private static RelicCatalogue _catalogue;
        private readonly List<RelicData> _candidates = new();

        private void Awake()
        {
            if (_catalogue == null) _catalogue = Resources.Load<RelicCatalogue>(CatalogueResource);
            _regularDropChance = RemoteConfig.Float(RemoteConfig.Keys.RelicDropChance, _regularDropChance);
            if (_pool == null || _spawnDirector == null || _collector == null || _relicService == null || _relicPickupPrefab == null)
                Debug.LogError($"{nameof(RelicDropper)} on '{name}' is missing a reference.", this);
        }

        private void OnEnable()
        {
            if (_spawnDirector != null) _spawnDirector.EnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            if (_spawnDirector != null) _spawnDirector.EnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(EnemyBrain brain, Vector2 position, EnemyData data)
        {
            bool isBoss = brain != null && brain.TryGetComponent(out BossMarker _);
            bool shouldDrop = isBoss || Random.value < _regularDropChance;
            if (!shouldDrop) return;

            RelicData relic = PickUnownedRelic();
            if (relic == null || _pool == null || _relicPickupPrefab == null) return;

            Vector2 pos = position + Random.insideUnitCircle * 0.3f;
            GameObject go = _pool.Spawn(_relicPickupPrefab, pos, Quaternion.identity);
            if (go == null || !go.TryGetComponent(out RelicPickup pickup)) return;

            pickup.SetRelic(relic);
            pickup.Bind(_collector);
        }

        private RelicData PickUnownedRelic()
        {
            if (_catalogue == null || _relicService == null) return null;

            _candidates.Clear();
            foreach (var relic in _catalogue.relics)
                if (relic != null && !_relicService.Owns(relic)) _candidates.Add(relic);

            return _candidates.Count == 0 ? null : _candidates[Random.Range(0, _candidates.Count)];
        }
    }
}
