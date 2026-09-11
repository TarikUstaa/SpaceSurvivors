using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// A stationary damage field (M15): an ion storm cell, a mine cluster, a radiation pocket.
    /// On a fixed tick it overlaps a circle and damages every <see cref="IDamageable"/> inside
    /// — the player <b>and</b> enemies (positioning is the only defence, GDD). No physics
    /// layer, no rigidbody: a pure overlap ticker into a shared buffer, so it works the same
    /// whether it's placed by hand or streamed by the <see cref="EnvironmentDirector"/>.
    ///
    /// The child sprite (assigned in the prefab) just pulses for readability.
    /// </summary>
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public class HazardZone : MonoBehaviour, IPoolable
    {
        [Header("Damage")]
        [SerializeField, Min(0.1f)] private float _radius = 2.2f;
        [SerializeField, Min(0f)] private float _damagePerTick = 5f;
        [SerializeField, Min(0.1f)] private float _tickInterval = 0.6f;
        [SerializeField] private LayerMask _targetLayers;

        [Header("Visual (optional)")]
        [SerializeField] private Transform _pulse;
        [SerializeField, Min(0f)] private float _pulseAmount = 0.08f;
        [SerializeField, Min(0f)] private float _pulseSpeed = 2.4f;

        private float _nextTick;
        private Vector3 _pulseBaseScale = Vector3.one;
        private static readonly Collider2D[] _hits = new Collider2D[24];

        private void Awake()
        {
            if (_pulse != null) _pulseBaseScale = _pulse.localScale;
        }

        public void OnSpawned() => _nextTick = 0f;
        public void OnDespawned() { }

        private void Update()
        {
            if (_pulse != null && _pulseAmount > 0f)
            {
                float s = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmount;
                _pulse.localScale = _pulseBaseScale * s;
            }

            if (Time.time < _nextTick || _damagePerTick <= 0f) return;
            _nextTick = Time.time + _tickInterval;

            int n = Physics2D.OverlapCircle(transform.position, _radius,
                OverlapFilter.For(_targetLayers), _hits);
            for (int i = 0; i < n; i++)
            {
                var col = _hits[i];
                if (col == null) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive) continue;

                Vector2 dir = (Vector2)col.transform.position - (Vector2)transform.position;
                target.TakeDamage(new DamageInfo(_damagePerTick, gameObject, transform.position, dir));
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
