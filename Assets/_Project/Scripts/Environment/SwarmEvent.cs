using SpaceSurvivors.Core;
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
    ///
    /// The size scales with how long the run has lasted: a minute-one swarm is a scare, a
    /// minute-ten swarm is a genuine emergency (a strong build shredded the old flat 34 in
    /// two seconds, so it stopped registering).
    /// </summary>
    public class SwarmEvent : SpaceEventBehaviour
    {
        [Tooltip("Pool to draw from — repeat an entry to weight it. Mostly fast chaff.")]
        [SerializeField] private EnemyData[] _roster;

        [Header("Size (scales with run time)")]
        [Tooltip("Enemies in the very first swarm.")]
        [SerializeField, Min(1)] private int _baseEnemies = 30;
        [Tooltip("Extra enemies added per minute survived.")]
        [SerializeField, Min(0f)] private float _enemiesPerMinute = 9f;
        [Tooltip("Hard ceiling regardless of run length.")]
        [SerializeField, Min(1)] private int _maxEnemies = 150;

        [Tooltip("How many screen edges the swarm comes from.")]
        [SerializeField, Range(1, 3)] private int _edges = 2;
        [Tooltip("Seconds to ramp from the opening trickle up to the full pour.")]
        [SerializeField, Min(0.1f)] private float _rampInSeconds = 3f;
        [SerializeField] private Vector2Int _burstSize = new(2, 4);
        [Tooltip("Half-width of each edge's spawn line, world units.")]
        [SerializeField, Min(1f)] private float _edgeSpread = 6.5f;

        private Vector2[] _dirs;
        private int _quota;
        private int _spawned;
        private float _next;

        protected override void OnBegin()
        {
            _spawned = 0;
            _next = 0.6f;

            var clock = FindFirstObjectByType<RunClock>();
            float minutes = clock != null ? Mathf.Max(0f, clock.Elapsed) / 60f : 0f;
            _quota = Mathf.Clamp(
                _baseEnemies + Mathf.RoundToInt(_enemiesPerMinute * minutes),
                _baseEnemies, _maxEnemies);

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
            if (_spawned >= _quota) return;
            if (Elapsed < _next || Elapsed > Duration - 1.5f) return;

            // Bigger swarms need a heavier pour to actually land inside the window.
            int extra = _quota > 60 ? 2 : 0;
            int burst = Random.Range(_burstSize.x, _burstSize.y + 1) + extra;
            for (int b = 0; b < burst && _spawned < _quota; b++)
            {
                Vector2 dir = _dirs[Random.Range(0, _dirs.Length)];
                Vector2 perp = new(-dir.y, dir.x);
                Vector3 pos = OffscreenPoint(dir, 2f) + (Vector3)(perp * Random.Range(-_edgeSpread, _edgeSpread));

                var data = _roster[Random.Range(0, _roster.Length)];
                Ctx.Spawns.SpawnEnemyAt(data, pos);
                _spawned++;
            }

            // trickle at the start, then a steady heavy pour
            float pace = Mathf.Lerp(0.5f, 0.12f, Mathf.Clamp01(Elapsed / _rampInSeconds));
            _next = Elapsed + pace;
        }
    }
}
