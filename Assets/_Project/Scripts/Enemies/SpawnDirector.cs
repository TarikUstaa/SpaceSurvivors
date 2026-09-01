using System.Collections;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Drives continuous, time-based enemy spawning from a <see cref="DifficultyConfig"/>.
    /// Spawn rate and per-enemy stat multipliers are read off the config curves at the
    /// current <see cref="RunClock.Elapsed"/> — no discrete waves (GDD).
    ///
    /// Spawns come from the shared <see cref="PoolManager"/> (never Instantiate, §4). Keeps
    /// an accurate alive-count via the release callback it hands to each
    /// <see cref="EnemyBrain"/>, and honours <see cref="DifficultyConfig.maxAliveEnemies"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpawnDirector : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PoolManager _pool;
        [SerializeField] private RunClock _clock;
        [SerializeField] private Transform _player;
        [SerializeField] private DifficultyConfig _config;
        [SerializeField] private Camera _camera;

        [Header("Spawn placement")]
        [Tooltip("Extra world units beyond the screen edge that enemies appear at.")]
        [SerializeField, Min(0f)] private float _offscreenMargin = 2f;

        [Tooltip("Fraction of spawns placed in the arc the player is moving toward (the rest " +
                 "ring the screen uniformly). Keeps the horde in front of a fast kiter instead " +
                 "of spawning behind them where it's immediately left behind and far-culled.")]
        [SerializeField, Range(0f, 1f)] private float _leadBias = 0.6f;
        [Tooltip("Half-angle (degrees) of that lead arc, centred on the player's heading.")]
        [SerializeField, Range(10f, 180f)] private float _leadArcDegrees = 75f;

        [Header("Debug")]
        [SerializeField] private bool _logEverySpawn = false;

        private float _accumulator;
        private int _aliveCount;
        private Vector2 _lastPlayerPos;
        private Vector2 _playerHeading;
        private bool _haveLastPos;
        private bool[] _bossFired;
        private int _bossEntriesSpawned;
        private int _bossesAlive;

        public int AliveCount => _aliveCount;
        public float CurrentSpawnRate => _config != null ? _config.SpawnRateAt(Now) : 0f;

        /// <summary>How many scheduled bosses the player has killed this run.</summary>
        public int BossesDefeated { get; private set; }

        /// <summary>Total bosses in this run's schedule (0 for endless configs with none).</summary>
        public int ScheduledBossCount =>
            _config != null && _config.bossSchedule != null ? _config.bossSchedule.Count : 0;

        /// <summary>
        /// True once every entry in the boss schedule has spawned AND no boss is still alive.
        /// The Campaign win condition (ignored by endless modes, which have recurring bosses).
        /// </summary>
        public bool AllScheduledBossesDefeated =>
            _config != null && _config.bossSchedule != null && _config.bossSchedule.Count > 0
            && _bossEntriesSpawned >= _config.bossSchedule.Count
            && _bossesAlive == 0;

        /// <summary>(killed enemy, world position at death, its data). For loot / kill-count / VFX.</summary>
        public event System.Action<EnemyBrain, Vector2, Data.EnemyData> EnemyKilled;
        /// <summary>(boss display name, warning lead seconds) — raised when a boss is scheduled in.</summary>
        public event System.Action<string, float> BossIncoming;
        /// <summary>The boss brain, once it actually spawns.</summary>
        public event System.Action<EnemyBrain> BossSpawned;
        /// <summary>Raised when a scheduled boss is killed (for audio / stingers).</summary>
        public event System.Action BossDefeated;

        private float Now => _clock != null ? _clock.Elapsed : Time.timeSinceLevelLoad;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;

            // A mode picked in the menu overrides the serialized config; pressing Play
            // directly in the editor falls back to whatever is wired in the Inspector.
            if (GameSession.SelectedMode != null && GameSession.SelectedMode.difficulty != null)
                _config = GameSession.SelectedMode.difficulty;
        }

        private void Update()
        {
            if (_pool == null || _player == null || _config == null || _camera == null) return;

            float now = Now;
            TrackPlayerHeading();
            CheckBossSchedule(now);
            _accumulator += _config.SpawnRateAt(now) * Time.deltaTime;

            int guard = 0;
            while (_accumulator >= 1f && _aliveCount < _config.maxAliveEnemies && guard++ < 32)
            {
                _accumulator -= 1f;
                SpawnOne(now);
            }

            // Don't bank more than one pending spawn — avoids a burst after a frame hitch.
            if (_accumulator > 1f) _accumulator = 1f;
        }

        private void SpawnOne(float now)
        {
            EnemyData data = PickEnemy(now);
            if (data == null) return;
            SpawnEnemyAt(data, GetOffscreenPosition());
        }

        /// <summary>
        /// Spawn one extra enemy at a world position, fully wired into the alive-count and
        /// kill events (loot, kill-count, VFX all fire normally). Bypasses the spawn budget
        /// and the alive cap — same as bosses. Used by <see cref="SplitOnDeath"/>.
        /// </summary>
        public EnemyBrain SpawnEnemyAt(EnemyData data, Vector3 position)
        {
            if (_pool == null || _config == null || _player == null) return null;
            if (data == null || data.prefab == null) return null;

            GameObject go = _pool.Spawn(data.prefab, position, Quaternion.identity);
            if (go == null || !go.TryGetComponent(out EnemyBrain brain)) return null;

            float now = Now;
            float hp = data.baseHealth * _config.HealthMultiplierAt(now);
            float speedMul = _config.SpeedMultiplierAt(now);

            _aliveCount++;
            brain.Killed += HandleEnemyKilled;
            brain.Initialize(_player, data, hp, speedMul, OnEnemyReleased);

            if (_logEverySpawn)
                Debug.Log($"[Spawn] {data.displayName} @ {now:0}s  hp={hp:0}  alive={_aliveCount}  rate={_config.SpawnRateAt(now):0.0}/s");

            return brain;
        }

        // ---------------------------------------------------------------- bosses

        private void CheckBossSchedule(float now)
        {
            var schedule = _config.bossSchedule;
            if (schedule == null || schedule.Count == 0) return;

            if (_bossFired == null || _bossFired.Length != schedule.Count)
                _bossFired = new bool[schedule.Count];

            for (int i = 0; i < schedule.Count; i++)
            {
                var entry = schedule[i];
                if (_bossFired[i] || entry == null || entry.bossData == null) continue;
                if (now < entry.triggerTime) continue;

                _bossFired[i] = true;
                BossIncoming?.Invoke(entry.bossData.displayName, entry.warningLead);
                StartCoroutine(SpawnBossAfter(entry, now));
            }
        }

        private IEnumerator SpawnBossAfter(BossEntry entry, float scheduledAt)
        {
            if (entry.warningLead > 0f)
                yield return new WaitForSeconds(entry.warningLead);

            EnemyData data = entry.bossData;
            float hp = data.baseHealth * Mathf.Lerp(1f, _config.HealthMultiplierAt(scheduledAt), 0.5f);
            float speedMul = _config.SpeedMultiplierAt(scheduledAt);

            for (int n = 0; n < Mathf.Max(1, entry.count); n++)
            {
                GameObject go = _pool.Spawn(data.prefab, GetOffscreenPosition(), Quaternion.identity);
                if (go == null || !go.TryGetComponent(out EnemyBrain brain)) continue;

                _bossesAlive++;
                brain.Killed += HandleEnemyKilled;
                brain.Initialize(_player, data, hp, speedMul, OnBossReleased);
                BossSpawned?.Invoke(brain);
            }
            _bossEntriesSpawned++;
        }

        private void OnBossReleased(EnemyBrain brain)
        {
            brain.Killed -= HandleEnemyKilled;
            _bossesAlive = Mathf.Max(0, _bossesAlive - 1);
            BossesDefeated++;
            BossDefeated?.Invoke();
        }

        private void HandleEnemyKilled(EnemyBrain brain, DamageInfo _)
            => EnemyKilled?.Invoke(brain, brain.transform.position, brain.Data);

        private void OnEnemyReleased(EnemyBrain brain)
        {
            brain.Killed -= HandleEnemyKilled;
            _aliveCount = Mathf.Max(0, _aliveCount - 1);
        }

        private EnemyData PickEnemy(float now)
        {
            float total = 0f;
            var roster = _config.roster;
            for (int i = 0; i < roster.Count; i++)
            {
                var e = roster[i];
                if (e != null && now >= e.earliestSpawnTime)
                    total += Mathf.Max(0f, e.spawnWeight);
            }
            if (total <= 0f) return null;

            float r = Random.value * total;
            for (int i = 0; i < roster.Count; i++)
            {
                var e = roster[i];
                if (e == null || now < e.earliestSpawnTime) continue;
                r -= Mathf.Max(0f, e.spawnWeight);
                if (r <= 0f) return e;
            }
            return null;
        }

        private void TrackPlayerHeading()
        {
            Vector2 p = _player.position;
            if (_haveLastPos)
            {
                Vector2 delta = p - _lastPlayerPos;
                if (delta.sqrMagnitude > 1e-4f)
                    _playerHeading = Vector2.Lerp(_playerHeading, delta.normalized, 0.12f).normalized;
            }
            _lastPlayerPos = p;
            _haveLastPos = true;
        }

        private Vector3 GetOffscreenPosition()
        {
            float h = _camera.orthographicSize;
            float w = h * _camera.aspect;
            Vector2 c = _camera.transform.position;
            float m = _offscreenMargin;

            // Pick a compass direction: mostly the arc the player is heading into (so the
            // spawn stays relevant), the rest anywhere around the screen.
            float ang;
            if (_playerHeading.sqrMagnitude > 1e-4f && Random.value < _leadBias)
            {
                float baseAng = Mathf.Atan2(_playerHeading.y, _playerHeading.x);
                ang = baseAng + Random.Range(-_leadArcDegrees, _leadArcDegrees) * Mathf.Deg2Rad;
            }
            else
            {
                ang = Random.value * Mathf.PI * 2f;
            }

            // Cast that direction out to the screen rectangle + margin.
            Vector2 dir = new(Mathf.Cos(ang), Mathf.Sin(ang));
            float tx = (w + m) / Mathf.Max(1e-4f, Mathf.Abs(dir.x));
            float ty = (h + m) / Mathf.Max(1e-4f, Mathf.Abs(dir.y));
            float t = Mathf.Min(tx, ty);
            return new Vector3(c.x + dir.x * t, c.y + dir.y * t, 0f);
        }
    }
}
