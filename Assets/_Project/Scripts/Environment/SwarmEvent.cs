using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// Swarm event (goal G1) — after the banner warning, a wall of enemies pours in from one or
    /// two screen edges and rushes the player. They are ordinary enemies spawned through
    /// <see cref="SpaceSurvivors.Enemies.SpawnDirector.SpawnEnemyAt"/> (time-scaled HP/speed,
    /// count toward the alive tally, drop loot) — so the swarm is both a threat and a farming
    /// window, and it does <b>not</b> get auto-released when the event ends: you have to clear it.
    /// </summary>
    public class SwarmEvent : SpaceEventBehaviour
    {
        [Tooltip("Pool to draw from — repeat an entry to weight it. Mostly fast chaff.")]
        [SerializeField] private EnemyData[] _roster;
        [SerializeField, Min(1)] private int _totalEnemies = 34;
        [Tooltip("How many screen edges the swarm comes from.")]
        [SerializeField, Range(1, 3)] private int _edges = 2;
        [Tooltip("Seconds to ramp from the opening trickle up to the full pour.")]
        [SerializeField, Min(0.1f)] private float _rampInSeconds = 3f;
        [SerializeField] private Vector2Int _burstSize = new(2, 4);
        [Tooltip("Half-width of each edge's spawn line, world units.")]
        [SerializeField, Min(1f)] private float _edgeSpread = 6.5f;

        private Vector2[] _dirs;
        private int _spawned;
        private float _next;

        protected override void OnBegin()
        {
            _spawned = 0;
            _next = 0.6f;

            int n = Mathf.Clamp(_edges, 1, 3);
            _dirs = new Vector2[n];
            float a0 = Random.value * Mathf.PI * 2f;
            for (int i = 0; i < n; i++)
            {
                float a = a0 + i * (Mathf.PI * 2f / n) + Random.Range(-0.3f, 0.3f);
                _dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
        }

        protected override void OnTick(float dt)
        {
            if (Ctx.Spawns == null || _roster == null || _roster.Length == 0) return;
            if (_spawned >= _totalEnemies) return;
            if (Elapsed < _next || Elapsed > Duration - 1.5f) return;

            int burst = Random.Range(_burstSize.x, _burstSize.y + 1);
            for (int b = 0; b < burst && _spawned < _totalEnemies; b++)
            {
                Vector2 dir = _dirs[Random.Range(0, _dirs.Length)];
                Vector2 perp = new(-dir.y, dir.x);
                Vector3 pos = OffscreenPoint(dir, 2f) + (Vector3)(perp * Random.Range(-_edgeSpread, _edgeSpread));

                var data = _roster[Random.Range(0, _roster.Length)];
                Ctx.Spawns.SpawnEnemyAt(data, pos);
                _spawned++;
            }

            // trickle at the start, then a steady heavy pour
            float pace = Mathf.Lerp(0.55f, 0.16f, Mathf.Clamp01(Elapsed / _rampInSeconds));
            _next = Elapsed + pace;
        }
    }
}
