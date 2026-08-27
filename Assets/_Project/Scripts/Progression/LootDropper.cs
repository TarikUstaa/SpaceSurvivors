using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Enemies;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// Listens to <see cref="SpawnDirector.EnemyKilled"/> and drops one pooled
    /// <see cref="XpPickup"/> per kill, carrying that enemy's scrap value (one object, not
    /// N — cheaper and the pickup still grants the full amount). Lives next to the
    /// SpawnDirector on the systems object; the enemy prefab stays loot-agnostic
    /// (AI_Guidelines §1, §4).
    /// </summary>
    [DisallowMultipleComponent]
    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private PoolManager _pool;
        [SerializeField] private SpawnDirector _spawnDirector;
        [SerializeField] private ScrapCollector _collector;
        [SerializeField] private GameObject _scrapPickupPrefab;

        [Tooltip("XP granted per unit of scrap value.")]
        [SerializeField, Min(1)] private int _xpPerScrap = 1;

        private void Awake()
        {
            if (_pool == null || _spawnDirector == null || _collector == null || _scrapPickupPrefab == null)
                Debug.LogError($"{nameof(LootDropper)} on '{name}' is missing a reference.", this);
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
            int scrap = data != null ? data.scrapValue : 1;
            if (scrap <= 0 || _pool == null || _scrapPickupPrefab == null) return;

            GameObject go = _pool.Spawn(_scrapPickupPrefab, position, Quaternion.identity);
            if (go != null && go.TryGetComponent(out XpPickup pickup))
                pickup.Configure(_collector, scrap, scrap * _xpPerScrap);
        }
    }
}
