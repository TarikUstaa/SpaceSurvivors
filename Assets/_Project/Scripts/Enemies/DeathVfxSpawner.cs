using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Plays a pooled one-shot death effect where an enemy dies. Lives next to the
    /// <see cref="SpawnDirector"/> and listens to its <see cref="SpawnDirector.EnemyKilled"/>
    /// event, so the enemy prefab stays VFX-agnostic and nothing is Instantiated
    /// (AI_Guidelines §1, §4). Per-type effect comes from <see cref="EnemyData.deathVfxPrefab"/>,
    /// with a shared <see cref="_fallbackVfx"/> when a type doesn't set one.
    /// </summary>
    [DisallowMultipleComponent]
    public class DeathVfxSpawner : MonoBehaviour
    {
        [SerializeField] private PoolManager _pool;
        [SerializeField] private SpawnDirector _spawnDirector;
        [SerializeField] private GameObject _fallbackVfx;

        private void Awake()
        {
            if (_pool == null || _spawnDirector == null)
                Debug.LogError($"{nameof(DeathVfxSpawner)} on '{name}' is missing a reference.", this);
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
            GameObject prefab = data != null && data.deathVfxPrefab != null ? data.deathVfxPrefab : _fallbackVfx;
            if (prefab != null && _pool != null)
                _pool.Spawn(prefab, position, Quaternion.identity);
        }
    }
}
