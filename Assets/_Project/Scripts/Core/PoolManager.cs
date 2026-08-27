using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Scene service that owns one <see cref="Pool"/> per prefab. Systems that spawn things
    /// (weapons, spawn director, loot) hold a serialized reference to this and call
    /// <see cref="Spawn"/> — they never call <c>Instantiate</c> (AI_Guidelines §4).
    ///
    /// Not a global singleton: put ONE on a scene object and wire it via the Inspector so
    /// dependencies stay explicit and testable (AI_Guidelines §1).
    /// </summary>
    [DisallowMultipleComponent]
    public class PoolManager : MonoBehaviour
    {
        [System.Serializable]
        private struct PrewarmEntry
        {
            public GameObject prefab;
            [Min(0)] public int count;
        }

        [Tooltip("Pools created and filled on Awake so the first shot never hitches.")]
        [SerializeField] private List<PrewarmEntry> _prewarm = new();

        private readonly Dictionary<GameObject, Pool> _pools = new();
        private Transform _root;

        private void Awake()
        {
            _root = new GameObject("[Pooled]").transform;
            _root.SetParent(transform, false);

            foreach (var entry in _prewarm)
            {
                if (entry.prefab == null) continue;
                GetOrCreatePool(entry.prefab, entry.count);
            }
        }

        private Pool GetOrCreatePool(GameObject prefab, int prewarm = 0)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                var holder = new GameObject($"Pool_{prefab.name}").transform;
                holder.SetParent(_root, false);
                pool = new Pool(prefab, prewarm, holder);
                _pools.Add(prefab, pool);
            }
            return pool;
        }

        /// <summary>Get a live instance of <paramref name="prefab"/>. Creates the pool on first use.</summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            return GetOrCreatePool(prefab).Get(position, rotation);
        }

        /// <summary>Return any pooled object to its pool.</summary>
        public void Despawn(GameObject instance)
        {
            if (instance != null && instance.TryGetComponent(out PoolHandle handle))
                handle.Despawn();
            else if (instance != null)
                instance.SetActive(false);
        }
    }
}
