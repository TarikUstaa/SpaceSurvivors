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
            if (_run == null) _run = FindFirstObjectByType<RunController>();
            if (_stats == null) _stats = FindFirstObjectByType<RunStats>();
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
            // rule: you keep the scrap even on a defeat). One seam — ProfileService.
            if (_stats != null)
            {
                ProfileService.AddScrap(_stats.Scrap);
                ProfileService.RecordRun(_stats.Kills);
            }
            ProfileService.Save();

            if (_statsValue != null && _stats != null)
                _statsValue.text =
                    $"KILLS  {_stats.Kills}\nLEVEL  {_stats.Level}\n" +
                    $"SCRAP  +{_stats.Scrap}\nWALLET  {ProfileService.Wallet}";

            string modeId = GameSession.SelectedMode != null ? GameSession.SelectedMode.name : "default";
            bool record = HighScoreService.Submit(modeId, survivedSeconds);
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
