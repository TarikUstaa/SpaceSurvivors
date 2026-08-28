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

        [Header("Debug")]
        [SerializeField] private bool _logEverySpawn = false;

        private float _accumulator;
        private int _aliveCount;
        private bool[] _bossFired;
        private int _bossEntriesSpawned;
        private int _bossesAlive;

        public int AliveCount => _aliveCount;
        public float CurrentSpawnRate => _config != null ? _config.SpawnRateAt(Now) : 0f;

        /// <summary>How many scheduled bosses the player has killed this run.</summary>
        public int BossesDefeated { get; private set; }

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
            if (data == null || data.prefab == null) return;

            Vector3 pos = GetOffscreenPosition();
            GameObject go = _pool.Spawn(data.prefab, pos, Quaternion.identity);
            if (go == null || !go.TryGetComponent(out EnemyBrain brain)) return;

            float hp = data.baseHealth * _config.HealthMultiplierAt(now);
            float speedMul = _config.SpeedMultiplierAt(now);

            _aliveCount++;
            brain.Killed += HandleEnemyKilled;
            brain.Initialize(_player, data, hp, speedMul, OnEnemyReleased);

            if (_logEverySpawn)
                Debug.Log($"[Spawn] {data.displayName} @ {now:0}s  hp={hp:0}  alive={_aliveCount}  rate={_config.SpawnRateAt(now):0.0}/s");
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

        private Vector3 GetOffscreenPosition()
        {
            float h = _camera.orthographicSize;
            float w = h * _camera.aspect;
            Vector2 c = _camera.transform.position;
            float m = _offscreenMargin;

            switch (Random.Range(0, 4))
            {
                case 0:  return new Vector3(c.x - w - m, Random.Range(c.y - h, c.y + h), 0f); // left
                case 1:  return new Vector3(c.x + w + m, Random.Range(c.y - h, c.y + h), 0f); // right
                case 2:  return new Vector3(Random.Range(c.x - w, c.x + w), c.y + h + m, 0f); // top
                default: return new Vector3(Random.Range(c.x - w, c.x + w), c.y - h - m, 0f); // bottom
            }
        }
    }
}
