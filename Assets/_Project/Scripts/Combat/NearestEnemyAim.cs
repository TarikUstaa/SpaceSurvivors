using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Auto-aim: points at the closest collider on <see cref="_enemyLayers"/> within range.
    /// Uses a non-alloc overlap query so it stays cheap even when fired every frame
    /// (AI_Guidelines §4). Falls back to <see cref="_fallbackToForward"/> when nothing is
    /// in range so the weapon still fires during the calm opening seconds.
    /// </summary>
    [DisallowMultipleComponent]
    public class NearestEnemyAim : MonoBehaviour, IAimStrategy
    {
        [SerializeField] private LayerMask _enemyLayers;
        [Tooltip("If no enemy is in range, fire in the ship's 'up' direction instead of not firing.")]
        [SerializeField] private bool _fallbackToForward = true;

        private static readonly Collider2D[] Hits = new Collider2D[32];

        private ContactFilter2D _filter;
        private bool _filterReady;

        private void Awake() => BuildFilter();

        private void BuildFilter()
        {
            _filter = new ContactFilter2D { useLayerMask = true, useTriggers = true };
            _filter.SetLayerMask(_enemyLayers);
            _filterReady = true;
        }

        public bool TryGetAimDirection(Vector2 origin, float range, out Vector2 direction)
        {
            if (!_filterReady) BuildFilter();
            int count = Physics2D.OverlapCircle(origin, range, _filter, Hits);

            float bestSqr = float.MaxValue;
            Vector2 best = Vector2.zero;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                var c = Hits[i];
                if (c == null || !c.gameObject.activeInHierarchy) continue;

                Vector2 delta = (Vector2)c.transform.position - origin;
                float sqr = delta.sqrMagnitude;
                if (sqr < bestSqr && sqr > 0.0001f)
                {
                    bestSqr = sqr;
                    best = delta;
                    found = true;
                }
            }

            if (found)
            {
                direction = best.normalized;
                return true;
            }

            if (_fallbackToForward)
            {
                direction = transform.up;
                return true;
            }

            direction = Vector2.zero;
            return false;
        }
    }
}
