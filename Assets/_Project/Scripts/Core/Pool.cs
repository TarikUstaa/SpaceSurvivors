using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// A recycler for one prefab. Plain C# (no MonoBehaviour) so it is easy to test and to
    /// own from a manager. Never destroys objects during play — inactive instances sit in
    /// <see cref="_inactive"/> waiting to be reused (AI_Guidelines §4).
    /// </summary>
    public sealed class Pool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Stack<GameObject> _inactive = new();
        private readonly bool _expandable;

        public int CountInactive => _inactive.Count;
        public int CountAll { get; private set; }

        public Pool(GameObject prefab, int prewarm, Transform parent, bool expandable = true)
        {
            _prefab = prefab;
            _parent = parent;
            _expandable = expandable;
            for (int i = 0; i < prewarm; i++)
                _inactive.Push(CreateInstance());
        }

        private GameObject CreateInstance()
        {
            // The ONLY Instantiate call in the projectile/enemy pipeline, and it happens at
            // load/prewarm time, never mid-combat.
            GameObject go = Object.Instantiate(_prefab, _parent);
            go.SetActive(false);

            if (!go.TryGetComponent(out PoolHandle handle))
                handle = go.AddComponent<PoolHandle>();
            handle.Bind(this);

            CountAll++;
            return go;
        }

        /// <summary>Take an instance, position it, activate it, and fire its spawn hooks.</summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject go;
            if (_inactive.Count > 0)
                go = _inactive.Pop();
            else if (_expandable)
                go = CreateInstance();
            else
                return null;

            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);

            var poolables = go.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++)
                poolables[i].OnSpawned();

            return go;
        }

        /// <summary>Return an instance to the pool. Safe to call twice.</summary>
        public void Release(GameObject go)
        {
            if (go == null || !go.activeSelf) return;

            var poolables = go.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++)
                poolables[i].OnDespawned();

            go.SetActive(false);
            if (_parent != null) go.transform.SetParent(_parent, false);
            _inactive.Push(go);
        }
    }
}
