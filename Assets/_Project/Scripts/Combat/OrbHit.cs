using System.Collections.Generic;
using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// One orbiting damage ball. It is moved by <see cref="OrbitalWeapon"/>; all it does is
    /// hurt each enemy it overlaps, no more than once per <see cref="_interval"/> seconds
    /// (a per-target cooldown, like <see cref="ContactDamage"/> but player-owned). Pooled.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class OrbHit : MonoBehaviour, IPoolable
    {
        private float _damage;
        private float _interval = 0.4f;
        private GameObject _owner;
        private readonly Dictionary<IDamageable, float> _nextHit = new();

        public void Configure(float damage, float interval, GameObject owner)
        {
            _damage = damage;
            _interval = Mathf.Max(0.05f, interval);
            _owner = owner;
        }

        public void OnSpawned() => _nextHit.Clear();
        public void OnDespawned() => _nextHit.Clear();

        private void OnTriggerStay2D(Collider2D other) => TryHit(other);
        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);

        private void TryHit(Collider2D other)
        {
            if (_damage <= 0f) return;
            if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == _owner) return;

            var target = other.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive) return;

            if (_nextHit.TryGetValue(target, out float t) && Time.time < t) return;
            _nextHit[target] = Time.time + _interval;

            target.TakeDamage(new DamageInfo(_damage, _owner, transform.position, Vector2.zero));
        }
    }
}
