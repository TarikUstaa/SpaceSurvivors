using SpaceSurvivors.Core;
using SpaceSurvivors.Enemies;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// Read-only tally of the current run — kills, level reached, scrap earned, time survived.
    /// Sits on the Systems object next to <see cref="SpawnDirector"/>. It only observes
    /// (subscribes to <see cref="SpawnDirector.EnemyKilled"/>, reads sibling components); the
    /// end screen snapshots it when the run finishes (AI_Guidelines §1).
    /// </summary>
    [DisallowMultipleComponent]
    public class RunStats : MonoBehaviour
    {
        [SerializeField] private SpawnDirector _spawnDirector;
        [SerializeField] private LevelSystem _levelSystem;
        [SerializeField] private ScrapCollector _scrapCollector;
        [SerializeField] private RunClock _clock;

        public int Kills { get; private set; }
        public int Level => _levelSystem != null ? _levelSystem.CurrentLevel : 1;
        public int Scrap => _scrapCollector != null ? _scrapCollector.TotalScrap : 0;
        public float Seconds => _clock != null ? _clock.Elapsed : 0f;
        /// <summary>Scheduled bosses the player killed this run.</summary>
        public int BossesDefeated => _spawnDirector != null ? _spawnDirector.BossesDefeated : 0;

        private void Awake()
        {
            if (_spawnDirector == null) _spawnDirector = FindAnyObjectByType<SpawnDirector>();
            if (_levelSystem == null) _levelSystem = FindAnyObjectByType<LevelSystem>();
            if (_scrapCollector == null) _scrapCollector = FindAnyObjectByType<ScrapCollector>();
            if (_clock == null) _clock = FindAnyObjectByType<RunClock>();
        }

        private void OnEnable()
        {
            if (_spawnDirector != null) _spawnDirector.EnemyKilled += HandleKill;
        }

        private void OnDisable()
        {
            if (_spawnDirector != null) _spawnDirector.EnemyKilled -= HandleKill;
        }

        private void HandleKill(EnemyBrain brain, Vector2 pos, Data.EnemyData data) => Kills++;
    }
}
