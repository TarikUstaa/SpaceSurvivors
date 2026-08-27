using System;
using System.Collections;
using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Game
{
    /// <summary>
    /// Owns the run lifecycle: watches the player's <see cref="HealthComponent"/> and, on
    /// death, ends the run — stops the clock, halts spawning, eases into a freeze, and
    /// fires <see cref="RunEnded"/> for the UI. Nothing else needs to know "is the run
    /// over" logic; systems just subscribe or get toggled here (AI_Guidelines §1).
    ///
    /// The same pause plumbing (<see cref="Time.timeScale"/> = 0) will be reused by the
    /// M5 level-up screen.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunController : MonoBehaviour
    {
        [Header("Watched")]
        [SerializeField] private HealthComponent _playerHealth;

        [Header("Stopped on death")]
        [SerializeField] private RunClock _clock;
        [Tooltip("Behaviours disabled the moment the run ends (e.g. SpawnDirector).")]
        [SerializeField] private Behaviour[] _disableOnEnd;

        [Header("Feel")]
        [Tooltip("Seconds of unscaled time to ramp timeScale 1 → 0 after the player dies.")]
        [SerializeField, Min(0f)] private float _freezeRampDuration = 0.6f;

        /// <summary>Fired once when the run ends. Argument = seconds survived.</summary>
        public event Action<float> RunEnded;

        public bool RunOver { get; private set; }
        public float SurvivalSeconds => _clock != null ? _clock.Elapsed : 0f;

        private void Awake()
        {
            if (_playerHealth == null)
                Debug.LogError($"{nameof(RunController)} on '{name}' has no player HealthComponent assigned.", this);
        }

        private void OnEnable()
        {
            if (_playerHealth != null) _playerHealth.Died += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (_playerHealth != null) _playerHealth.Died -= HandlePlayerDied;
        }

        private void HandlePlayerDied(DamageInfo _)
        {
            if (RunOver) return;
            RunOver = true;

            if (_clock != null) _clock.Running = false;

            foreach (var b in _disableOnEnd)
                if (b != null) b.enabled = false;

            float survived = SurvivalSeconds;
            RunEnded?.Invoke(survived);

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
