using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using SpaceSurvivors.Progression;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// A rare escape pod / supply drop that drifts through the arena (M18). Fly into it and
    /// it pops: heals the ship a little and scatters a scrap burst, then despawns. Pooled and
    /// streamed like any other prop. No collider blocking — a plain trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public class BonusPod : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0f)] private float _driftSpeed = 0.5f;
        [SerializeField, Min(0f)] private float _healAmount = 20f;
        [SerializeField, Min(0)] private int _scrap = 10;
        [SerializeField] private GameObject _scrapPickupPrefab;
        [SerializeField] private GameObject _popVfxPrefab;

        private PoolHandle _handle;
        private PoolManager _pool;
        private ScrapCollector _collector;
        private Vector2 _drift;
        private bool _spent;

        private void Awake()
        {
            _handle = GetComponent<PoolHandle>();
            _pool = FindFirstObjectByType<PoolManager>();
            _collector = FindFirstObjectByType<ScrapCollector>();
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void OnSpawned()
        {
            _spent = false;
            float a = Random.value * Mathf.PI * 2f;
            _drift = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * _driftSpeed;
        }

        public void OnDespawned() => _drift = Vector2.zero;

        private void Update()
        {
            if (_drift != Vector2.zero) transform.position += (Vector3)(_drift * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_spent) return;
            var health = other.GetComponentInParent<HealthComponent>();
            if (health == null || _collector == null || other.GetComponentInParent<ScrapCollector>() == null)
                return; // player only (the ship owns the ScrapCollector)

            _spent = true;
            if (_healAmount > 0f) health.Heal(_healAmount);

            int drops = Mathf.Clamp(_scrap / 4, 1, 6);
            int each = Mathf.Max(1, Mathf.RoundToInt(_scrap / (float)drops));
            for (int i = 0; i < drops && _scrapPickupPrefab != null && _pool != null; i++)
            {
                var go = _pool.Spawn(_scrapPickupPrefab, transform.position + (Vector3)(Random.insideUnitCircle * 0.8f), Quaternion.identity);
                if (go != null && go.TryGetComponent(out XpPickup pk)) pk.Configure(_collector, each, each);
            }

            if (_popVfxPrefab != null && _pool != null)
                _pool.Spawn(_popVfxPrefab, transform.position, Quaternion.identity);
            _handle.Despawn();
        }
    }
}
