using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// A stationary mine dropped by <see cref="MineLayer"/>. Arms after a short delay, then
    /// detonates (a full-damage <see cref="Aoe.Splash"/>) on the first enemy that touches it,
    /// or when its lifetime runs out. Pooled, never Destroyed (§4).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public class Mine : MonoBehaviour, IPoolable
    {
        private const float ArmDelay = 0.3f;

        private PoolHandle _handle;
        private float _damage;
        private float _radius;
        private float _life;
        private GameObject _owner;
        private GameObject _vfx;
        private PoolManager _pool;

        private float _age;
        private bool _spent;

        private void Awake() => _handle = GetComponent<PoolHandle>();

        public void Configure(float damage, float radius, float life, GameObject owner, GameObject vfx, PoolManager pool)
        {
            _damage = damage;
            _radius = radius;
            _life = life;
            _owner = owner;
            _vfx = vfx;
            _pool = pool;
        }

        public void OnSpawned() { _age = 0f; _spent = false; }
        public void OnDespawned() { }

        private void Update()
        {
            _age += Time.deltaTime;
            if (!_spent && _age >= _life) Detonate();
        }

        private void OnTriggerEnter2D(Collider2D other) => TryTrigger(other);
        private void OnTriggerStay2D(Collider2D other) => TryTrigger(other);

        private void TryTrigger(Collider2D other)
        {
            if (_spent || _age < ArmDelay) return;
            if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == _owner) return;

            var d = other.GetComponentInParent<IDamageable>();
            if (d == null || !d.IsAlive) return;

            Detonate();
        }

        private void Detonate()
        {
            _spent = true;
            Aoe.Splash(transform.position, _radius, _damage, _owner);
            if (_vfx != null && _pool != null)
                _pool.Spawn(_vfx, transform.position, Quaternion.identity);
            _handle.Despawn();
        }
    }
}
