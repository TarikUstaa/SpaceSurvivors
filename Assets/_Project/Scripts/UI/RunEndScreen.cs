using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Game;
using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Victory / Defeat panel. Reacts to <see cref="RunController.RunEnded"/> — swaps the
    /// header sprite for the outcome, shows the survival time, the run's stats (kills, level,
    /// scrap) and the mode's best time, and offers Replay / Menu.
    /// A real prefab-style component: it wires child references set in the Inspector, it
    /// does not build its own hierarchy (AI_Guidelines §7 — bootstrap-in-code is retired).
    /// </summary>
    [DisallowMultipleComponent]
    public class RunEndScreen : MonoBehaviour
    {
        [SerializeField] private RunController _run;
        [SerializeField] private RunStats _stats;

        [Header("Panel")]
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _headerImage;
        [SerializeField] private Sprite _winHeader;
        [SerializeField] private Sprite _loseHeader;
        [SerializeField] private Text _scoreValue;
        [SerializeField] private Text _statsValue;
        [SerializeField] private Text _bestValue;

        [Header("Buttons")]
        [SerializeField] private Button _replayButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private string _menuSceneName = "MainMenu";

        private void Awake()
        {
            if (_run == null) _run = FindAnyObjectByType<RunController>();
            if (_stats == null) _stats = FindAnyObjectByType<RunStats>();
            if (_root != null) _root.SetActive(false);
            if (_replayButton != null) _replayButton.onClick.AddListener(Replay);
            if (_menuButton != null) _menuButton.onClick.AddListener(ToMenu);
        }

        private void OnEnable()
        {
            if (_run != null) _run.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            if (_run != null) _run.RunEnded -= HandleRunEnded;
        }

        private void HandleRunEnded(bool won, float survivedSeconds)
        {
            if (_headerImage != null)
                _headerImage.sprite = won ? _winHeader : _loseHeader;

            if (_scoreValue != null)
                _scoreValue.text = Clock(survivedSeconds);

            // Bank the run's earnings + history before showing the numbers (Vampire-Survivors
            // rule: you keep the scrap even on a defeat). One seam — ProfileService. Only a share
            // of what was collected is banked, so display what came back, never _stats.Scrap.
            long banked = 0;
            if (_stats != null)
            {
                banked = ProfileService.BankRunScrap(_stats.Scrap);
                ProfileService.RecordRun(_stats.Kills, _stats.Level, survivedSeconds, _stats.BossesDefeated);
            }
            ProfileService.Save();

            // Lifetime stats are now up to date — unlock any achievements they earned (M14c).
            AchievementService.Evaluate();

            if (_statsValue != null && _stats != null)
                _statsValue.text =
                    $"KILLS  {_stats.Kills:n0}\nLEVEL  {_stats.Level}\n" +
                    // "of" the run total on purpose: the HUD counted every piece picked up, only a
                    // share of it is banked, and a bare "+750" after a run that showed 5,000 reads
                    // as a bug rather than a rule.
                    $"SCRAP  +{banked:n0} of {_stats.Scrap:n0}\nWALLET  {ProfileService.Wallet:n0}";

            string modeId = GameSession.SelectedMode != null ? GameSession.SelectedMode.name : "default";
            var runResult = new RunResult(
                survivedSeconds,
                _stats != null ? _stats.Kills : 0,
                _stats != null ? _stats.Level : 1,
                _stats != null ? _stats.BossesDefeated : 0);
            bool record = HighScoreService.Submit(modeId, runResult);
            if (_bestValue != null)
            {
                float best = HighScoreService.BestSeconds(modeId);
                _bestValue.text = record ? $"NEW BEST  {Clock(best)}" : $"BEST  {Clock(best)}";
            }

            if (_root != null) _root.SetActive(true);
        }

        private static string Clock(float seconds)
        {
            int t = Mathf.FloorToInt(Mathf.Max(0f, seconds));
            return $"{t / 60:0}:{t % 60:00}";
        }

        private void Replay()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_menuSceneName);
        }
    }
}
