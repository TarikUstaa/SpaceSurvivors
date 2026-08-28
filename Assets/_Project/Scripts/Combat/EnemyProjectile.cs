using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// A pooled enemy shot. The mirror image of <see cref="Projectile"/> but deliberately
    /// simpler: it only ever looks for an <see cref="IDamageable"/> on <see cref="_targetLayers"/>
    /// (the player), never pierces, and carries no stat pipeline — an enemy weapon pushes the
    /// final numbers in through <see cref="Launch"/> (AI_Guidelines §1, §3).
    ///
    /// Prefab setup: kinematic Rigidbody2D + trigger Collider2D on the EnemyProjectile layer.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public class EnemyProjectile : MonoBehaviour, IPoolable
    {
        [Tooltip("Layers this shot can hit (normally just Player).")]
        [SerializeField] private LayerMask _targetLayers;

        [Tooltip("Optional pooled one-shot VFX played where the shot lands / expires.")]
        [SerializeField] private GameObject _impactVfxPrefab;

        private Rigidbody2D _body;
        private PoolHandle _handle;

        private float _damage;
        private float _lifeLeft;
        private GameObject _owner;
        private PoolManager _pool;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _handle = GetComponent<PoolHandle>();
            GetComponent<Collider2D>().isTrigger = true;
        }

        /// <summary>Called by the firing enemy right after the pool spawns it.</summary>
        public void Launch(Vector2 direction, float speed, float damage, float lifetime,
                           GameObject owner, PoolManager pool)
        {
            _damage = damage;
            _lifeLeft = lifetime;
            _owner = owner;
            _pool = pool;

            Vector2 v = direction.normalized * speed;
            _body.linearVelocity = v;

            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void OnSpawned()
        {
            _lifeLeft = 0f;
            if (_body != null) _body.linearVelocity = Vector2.zero;
        }

        public void OnDespawned()
        {
            _owner = null;
            if (_body != null) _body.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            _lifeLeft -= Time.deltaTime;
            if (_lifeLeft <= 0f) _handle.Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0) return;
            if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == _owner) return;

            var target = other.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive) return;

            if (!target.TakeDamage(new DamageInfo(_damage, _owner, transform.position, _body.linearVelocity)))
                return; // hit didn't land (i-frames / shield already popped) — fly on

            SpawnImpact();
            _handle.Despawn();
        }

        private void SpawnImpact()
        {
            if (_impactVfxPrefab == null || _pool == null) return;
            _pool.Spawn(_impactVfxPrefab, transform.position, Quaternion.identity);
        }
    }
}
