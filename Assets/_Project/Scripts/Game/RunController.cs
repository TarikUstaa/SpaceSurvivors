using System;
using System.Collections;
using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Enemies;
using UnityEngine;

namespace SpaceSurvivors.Game
{
    /// <summary>
    /// Owns the run lifecycle. Ends the run on player death (defeat) or — for a non-endless
    /// mode — when every scheduled boss is dead (victory): stops the clock, halts spawning,
    /// eases into a freeze, and fires <see cref="RunEnded"/> for the UI. Nothing else needs
    /// "is the run over" logic; systems just subscribe or get toggled here (AI_Guidelines §1).
    /// The pause plumbing (<see cref="Time.timeScale"/> = 0) is shared with the level-up screen.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunController : MonoBehaviour
    {
        [Header("Watched")]
        [SerializeField] private HealthComponent _playerHealth;
        [SerializeField] private SpawnDirector _spawnDirector;

        [Header("Stopped on run end")]
        [SerializeField] private RunClock _clock;
        [Tooltip("Behaviours disabled the moment the run ends (e.g. SpawnDirector).")]
        [SerializeField] private Behaviour[] _disableOnEnd;

        [Header("Feel")]
        [Tooltip("Seconds of unscaled time to ramp timeScale 1 → 0 after the run ends.")]
        [SerializeField, Min(0f)] private float _freezeRampDuration = 0.6f;

        /// <summary>Fired once when the run ends. (won?, seconds survived).</summary>
        public event Action<bool, float> RunEnded;

        public bool RunOver { get; private set; }
        public bool Won { get; private set; }
        public float SurvivalSeconds => _clock != null ? _clock.Elapsed : 0f;

        private void Awake()
        {
            if (_playerHealth == null)
                Debug.LogError($"{nameof(RunController)} on '{name}' has no player HealthComponent assigned.", this);
            if (_spawnDirector == null) _spawnDirector = FindAnyObjectByType<SpawnDirector>();
        }

        private void OnEnable()
        {
            if (_playerHealth != null) _playerHealth.Died += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (_playerHealth != null) _playerHealth.Died -= HandlePlayerDied;
        }

        private void Update()
        {
            if (RunOver || GameSession.IsEndless || _spawnDirector == null) return;
            if (_spawnDirector.AllScheduledBossesDefeated)
                EndRun(won: true);
        }

        private void HandlePlayerDied(DamageInfo _) => EndRun(won: false);

        private void EndRun(bool won)
        {
            if (RunOver) return;
            RunOver = true;
            Won = won;

            if (_clock != null) _clock.Running = false;

            foreach (var b in _disableOnEnd)
                if (b != null) b.enabled = false;

            RunEnded?.Invoke(won, SurvivalSeconds);
            StartCoroutine(FreezeRamp());
        }

        private IEnumerator FreezeRamp()
        {
            float t = 0f;
            float start = Time.timeScale;
            while (t < _freezeRampDuration)
            {
                t += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(start, 0f, t / _freezeRampDuration);
                yield return null;
            }
            Time.timeScale = 0f;
        }
    }
}
