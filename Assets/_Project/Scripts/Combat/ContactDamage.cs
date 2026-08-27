using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Deals damage to any <see cref="IDamageable"/> on <see cref="_targetLayers"/> that it
    /// stays in contact with, on a fixed interval. Used by enemies against the player, but
    /// deliberately generic — a hazard or a boss aura could reuse it (AI_Guidelines §1).
    ///
    /// Values are pushed in via <see cref="Configure"/> by whatever owns it (e.g.
    /// <see cref="SpaceSurvivors.Enemies.EnemyBrain"/> from EnemyData), so there are no
    /// balance constants baked in here (§3). Zero-allocation: single-target cooldown only.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class ContactDamage : MonoBehaviour
    {
        [SerializeField] private LayerMask _targetLayers;
        [Tooltip("Fallback values for standalone use; normally overwritten by Configure().")]
        [SerializeField] private float _damage = 8f;
        [SerializeField] private float _interval = 0.5f;

        private float _nextHitTime;

        public void Configure(float damage, float interval)
        {
            _damage = Mathf.Max(0f, damage);
            _interval = Mathf.Max(0.05f, interval);
        }

        private void OnEnable() => _nextHitTime = 0f;

        // Works whether our collider is a trigger or solid — enemies are solid (so they
        // physically block the player) but still deal contact damage on the same interval.
        private void OnTriggerStay2D(Collider2D other) => TryHit(other);
        private void OnCollisionStay2D(Collision2D collision) => TryHit(collision.collider);
        private void OnCollisionEnter2D(Collision2D collision) => TryHit(collision.collider);

        private void TryHit(Collider2D other)
        {
            if (Time.time < _nextHitTime) return;
            if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

            var target = other.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive) return;

            _nextHitTime = Time.time + _interval;

            Vector2 dir = (Vector2)(other.transform.position - transform.position);
            target.TakeDamage(new DamageInfo(_damage, gameObject, transform.position, dir));
        }
    }
}
