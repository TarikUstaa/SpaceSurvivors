using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Boids-style separation: pushes away from nearby enemies so a swarm forms a crowd
    /// around the player instead of collapsing onto one point. Pure steering — no physics
    /// collisions between enemies (those stay disabled for performance, AI_Guidelines §4).
    ///
    /// Uses a non-alloc overlap query capped at <see cref="MaxNeighbours"/>; if enemy counts
    /// ever make this a hotspot, swap the query for a shared spatial hash — nothing else
    /// changes.
    /// </summary>
    [DisallowMultipleComponent]
    public class SeparationSteering : MonoBehaviour, IVelocityModifier
    {
        private const int MaxNeighbours = 12;

        [Tooltip("Neighbours within this radius contribute a push.")]
        [SerializeField, Min(0.05f)] private float _radius = 0.6f;

        [Tooltip("Push strength as a fraction of the enemy's top speed.")]
        [SerializeField, Range(0f, 3f)] private float _strength = 1.1f;

        [Tooltip("Which layers count as 'other enemies'.")]
        [SerializeField] private LayerMask _enemyLayers;

        private static readonly Collider2D[] Neighbours = new Collider2D[MaxNeighbours];

        private ContactFilter2D _filter;
        private Vector2 _jitter;

        private void Awake()
        {
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                useTriggers = true,
            };
            _filter.SetLayerMask(_enemyLayers);

            // Stable per-instance direction used only when two enemies perfectly overlap.
            float a = Random.value * Mathf.PI * 2f;
            _jitter = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        public Vector2 Modify(Vector2 desiredVelocity, Vector2 selfPosition, float maxSpeed, float deltaTime)
        {
            int count = Physics2D.OverlapCircle(selfPosition, _radius, _filter, Neighbours);
            if (count <= 1) return desiredVelocity; // only ourselves

            Vector2 push = Vector2.zero;
            int contributors = 0;

            for (int i = 0; i < count; i++)
            {
                var other = Neighbours[i];
                if (other == null || other.transform == transform) continue;

                Vector2 away = selfPosition - (Vector2)other.transform.position;
                float dist = away.magnitude;
                if (dist < 0.0001f)
                {
                    away = _jitter;
                    dist = 1f;
                }

                // Closer neighbours push harder (linear falloff to the radius edge).
                push += away / dist * (1f - Mathf.Clamp01(dist / _radius));
                contributors++;
            }

            if (contributors == 0) return desiredVelocity;

            push = push.normalized * (maxSpeed * _strength);
            Vector2 result = desiredVelocity + push;

            // Never let separation fling an enemy far faster than it can normally move.
            float cap = maxSpeed * 1.5f;
            if (result.sqrMagnitude > cap * cap)
                result = result.normalized * cap;

            return result;
        }
    }
}
