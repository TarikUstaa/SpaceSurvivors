using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// A pooled projectile. Moves in a straight line, damages the first
    /// <see cref="IDamageable"/> it overlaps, and returns itself to the pool on hit,
    /// on leaving its pierce budget, or on lifetime expiry.
    ///
    /// * <see cref="IPoolable"/> — fully reset on spawn, nothing leaks between uses (§4).
    /// * Never Destroys itself — always <see cref="PoolHandle.Despawn"/> (§4).
    /// * Knows nothing about weapons or enemies beyond the <see cref="IDamageable"/>
    ///   interface (§1, §2).
    /// * A hit that <b>doesn't land</b> (target already dead this frame) does NOT consume
    ///   the projectile — so a 3-shot fan doesn't all vanish on one weak enemy.
    ///
    /// Prefab setup: kinematic Rigidbody2D + trigger Collider2D on the PlayerProjectile
    /// layer; the layer collision matrix decides what it can hit.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class Projectile : MonoBehaviour, IPoolable
    {
        private Rigidbody2D _body;
        private PoolHandle _handle;

        private Vector2 _velocity;
        private float _damage;
        private int _pierceLeft;
        private float _lifeLeft;
        private GameObject _owner;

        private PoolManager _pool;
        private GameObject _impactVfx;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            // Physics integrates the constant velocity at a fixed step (framerate-independent);
            // interpolation keeps it smooth, continuous detection stops fast shots tunnelling.
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _handle = GetComponent<PoolHandle>();
            GetComponent<Collider2D>().isTrigger = true;
        }

        /// <summary>
        /// Called by <see cref="WeaponController"/> right after the pool spawns it.
        /// <paramref name="shot"/> carries the already-modified numbers (base × StatSheet),
        /// so the projectile never touches the stat pipeline.
        /// <paramref name="pool"/> is used only to spawn the impact VFX (may be null).
        /// </summary>
        public void Launch(Vector2 direction, WeaponData data, ShotParams shot, PoolManager pool, GameObject owner)
        {
            _velocity = direction.normalized * shot.Speed;
            _damage = shot.Damage;
            _pierceLeft = shot.Pierce;
            _lifeLeft = shot.Lifetime;
            _owner = owner;
            _pool = pool;
            _impactVfx = data.impactVfxPrefab;

            // Set velocity once — the 2D solver moves the body at a constant rate.
            _body.linearVelocity = _velocity;

            // Point the sprite along travel (Kenney lasers are drawn pointing up → +90).
            float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void OnSpawned()
        {
            _velocity = Vector2.zero;
            _lifeLeft = 0f;
            if (_body != null) _body.linearVelocity = Vector2.zero;
        }

        public void OnDespawned()
        {
            _velocity = Vector2.zero;
            _owner = null;
            if (_body != null) _body.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            _lifeLeft -= Time.deltaTime;
            if (_lifeLeft <= 0f)
                _handle.Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == _owner)
                return;

            // Collider may sit on a child of the entity — search upward.
            var target = other.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive) return;

            // Only pay the projectile if the hit actually landed. If another shot already
            // killed this target this frame, TakeDamage returns false and we fly on.
            if (!target.TakeDamage(new DamageInfo(_damage, _owner, transform.position, _velocity)))
                return;

            SpawnImpact();

            if (_pierceLeft > 0) _pierceLeft--;
            else _handle.Despawn();
        }

        private void SpawnImpact()
        {
            if (_impactVfx == null || _pool == null) return;
            _pool.Spawn(_impactVfx, transform.position, Quaternion.identity);
        }
    }
}
