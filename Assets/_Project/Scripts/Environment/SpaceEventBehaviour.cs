using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Progression;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// Shared plumbing for the M18 space events: keeps the <see cref="SpaceEventContext"/>,
    /// runs for <see cref="SpaceEventContext.Duration"/> seconds, then releases everything it
    /// spawned and returns itself to the pool. Concrete events override the three hooks and
    /// spawn through <see cref="SpawnChild"/> so cleanup is automatic (AI_Guidelines §4).
    /// </summary>
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public abstract class SpaceEventBehaviour : MonoBehaviour, ISpaceEvent, IPoolable
    {
        protected SpaceEventContext Ctx { get; private set; }
        protected float Elapsed { get; private set; }
        protected float Duration => Ctx.Duration;

        private PoolHandle _handle;
        private readonly List<GameObject> _spawned = new();
        private bool _running;

        private void Awake() => _handle = GetComponent<PoolHandle>();

        public void Begin(in SpaceEventContext context)
        {
            Ctx = context;
            Elapsed = 0f;
            _running = true;
            OnBegin();
        }

        public void OnSpawned() { }

        public void OnDespawned()
        {
            _running = false;
            ReleaseChildren();
        }

        private void Update()
        {
            if (!_running) return;

            Elapsed += Time.deltaTime;
            OnTick(Time.deltaTime);

            if (Elapsed >= Duration)
            {
                _running = false;
                OnEnd();
                _handle.Despawn();
            }
        }

        /// <summary>Spawn a pooled object that will be released automatically when the event ends.</summary>
        protected GameObject SpawnChild(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            if (prefab == null || Ctx.Pool == null) return null;
            var go = Ctx.Pool.Spawn(prefab, pos, rot);
            if (go != null) _spawned.Add(go);
            return go;
        }

        private void ReleaseChildren()
        {
            foreach (var go in _spawned)
                if (go != null && go.activeInHierarchy) Ctx.Pool.Despawn(go);
            _spawned.Clear();
        }

        protected Vector3 PlayerPos => Ctx.Player != null ? Ctx.Player.position : transform.position;

        /// <summary>Drop a configured scrap pickup so it actually flies to the ship.</summary>
        protected void DropScrap(GameObject pickupPrefab, Vector3 pos, int scrap, int xp)
        {
            if (pickupPrefab == null || Ctx.Pool == null) return;
            var go = Ctx.Pool.Spawn(pickupPrefab, pos, Quaternion.identity);
            if (go != null && go.TryGetComponent(out XpPickup pk))
                pk.Configure(Ctx.Collector, scrap, xp);
        }

        /// <summary>A point just off the visible edge, in the given screen-space direction.</summary>
        protected Vector3 OffscreenPoint(Vector2 dir, float margin = 3f)
        {
            var cam = Ctx.Camera;
            float h = cam != null ? cam.orthographicSize : 6f;
            float w = h * (cam != null ? cam.aspect : 1.78f);
            Vector3 c = PlayerPos;
            return c + new Vector3(dir.x * (w + margin), dir.y * (h + margin), 0f);
        }

        protected virtual void OnBegin() { }
        protected virtual void OnTick(float dt) { }
        protected virtual void OnEnd() { }
    }
}
