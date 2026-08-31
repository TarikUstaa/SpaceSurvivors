using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// M18 event — a stream of fast asteroids drifts across the arena from one edge for the
    /// event's duration. They are ordinary <see cref="Obstacle"/>s (destructible or not, per
    /// the prefab) with a directed drift, so they still block, take hits and drop scrap.
    /// </summary>
    public class MeteorShowerEvent : SpaceEventBehaviour
    {
        [SerializeField] private GameObject _meteorPrefab;
        [SerializeField, Min(0.05f)] private float _interval = 0.55f;
        [SerializeField] private Vector2 _speedRange = new(6f, 10f);
        [Tooltip("Half-width of the spawn line, world units, perpendicular to the drift.")]
        [SerializeField, Min(1f)] private float _spread = 9f;

        private Vector2 _dir;
        private Vector2 _perp;
        private float _next;

        protected override void OnBegin()
        {
            float a = Random.value * Mathf.PI * 2f;
            _dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            _perp = new Vector2(-_dir.y, _dir.x);
            _next = 0f;
        }

        protected override void OnTick(float dt)
        {
            // Ease the stream in and out so it isn't a hard wall of rock.
            if (Elapsed < _next || Elapsed > Duration - 2f) return;
            _next = Elapsed + _interval;

            Vector3 edge = OffscreenPoint(-_dir, 4f);
            edge += (Vector3)(_perp * Random.Range(-_spread, _spread));

            var go = SpawnChild(_meteorPrefab, edge, Quaternion.identity);
            if (go != null && go.TryGetComponent(out Obstacle ob))
                ob.SetDrift(_dir * Random.Range(_speedRange.x, _speedRange.y));
        }
    }
}
