using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Steering tweak (M19) that keeps a chasing enemy from grinding into asteroids, wrecks and
    /// other <see cref="SpaceSurvivors.Environment.Obstacle"/>s on the way to the player. A single
    /// forward <see cref="Physics2D.CircleCast"/> probes the path ahead; on a hit the enemy is
    /// nudged along the obstacle's surface (plus a small push off it) so it rounds the rock
    /// instead of stalling against it. Composes with <see cref="SeparationSteering"/> and the base
    /// <see cref="IMoveStrategy"/> via <see cref="EnemyBrain"/> — this class knows nothing about
    /// either (AI_Guidelines §1). Pure steering, no extra physics work beyond the one cast.
    /// </summary>
    [DisallowMultipleComponent]
    public class ObstacleAvoidance : MonoBehaviour, IVelocityModifier
    {
        private const int MaxHits = 4;

        [Tooltip("How far ahead (world units) to look for an obstacle.")]
        [SerializeField, Min(0.2f)] private float _lookAhead = 2.2f;

        [Tooltip("Radius of the forward probe — roughly the enemy's own radius plus clearance.")]
        [SerializeField, Min(0.05f)] private float _probeRadius = 0.4f;

        [Tooltip("Avoidance push strength as a fraction of the enemy's top speed.")]
        [SerializeField, Range(0f, 3f)] private float _strength = 1.4f;

        [Tooltip("Which layers block movement (the Obstacle layer).")]
        [SerializeField] private LayerMask _obstacleLayers;

        private static readonly RaycastHit2D[] Hits = new RaycastHit2D[MaxHits];

        private ContactFilter2D _filter;
        private Vector2 _sideBias;

        private void Awake()
        {
            _filter = new ContactFilter2D { useLayerMask = true, useTriggers = false };
            _filter.SetLayerMask(_obstacleLayers);

            // Stable per-instance tie-breaker for picking a side on a dead-on approach.
            float a = Random.value * Mathf.PI * 2f;
            _sideBias = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        public Vector2 Modify(Vector2 desiredVelocity, Vector2 selfPosition, float maxSpeed, float deltaTime)
        {
            float speed = desiredVelocity.magnitude;
            if (speed < 0.01f) return desiredVelocity;

            Vector2 dir = desiredVelocity / speed;

            int count = Physics2D.CircleCast(selfPosition, _probeRadius, dir, _filter, Hits, _lookAhead);
            if (count == 0) return desiredVelocity;

            // Nearest hit that we're actually driving into.
            RaycastHit2D hit = default;
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = Hits[i];
                if (h.collider == null || h.distance <= 0f) continue;       // started inside — ignore
                if (Vector2.Dot(desiredVelocity, -h.normal) <= 0f) continue; // already moving away
                if (h.distance < best) { best = h.distance; hit = h; }
            }
            if (hit.collider == null) return desiredVelocity;

            Vector2 n = hit.normal;
            float proximity = Mathf.Clamp01(1f - hit.distance / _lookAhead);

            // Two tangents along the surface; take the one that keeps us closest to our heading.
            Vector2 tangentA = new Vector2(-n.y, n.x);
            Vector2 tangent = Vector2.Dot(tangentA, dir) >= 0f ? tangentA : -tangentA;

            // Near head-on the tangent choice flip-flops — fall back to the stable per-enemy bias.
            if (Vector2.Dot(dir, -n) > 0.93f)
                tangent = Vector2.Dot(tangentA, _sideBias) >= 0f ? tangentA : -tangentA;

            // Cancel the part of our velocity pointing into the surface, then slide + push off.
            float into = Vector2.Dot(desiredVelocity, -n);
            if (into > 0f) desiredVelocity += n * (into * proximity);

            Vector2 steer = (tangent + n * 0.35f).normalized * (maxSpeed * _strength * proximity);
            Vector2 result = desiredVelocity + steer;

            float cap = maxSpeed * 1.1f;
            if (result.sqrMagnitude > cap * cap) result = result.normalized * cap;
            return result;
        }
    }
}
