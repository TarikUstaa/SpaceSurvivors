using SpaceSurvivors.Core;
using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// The career page — a full screen for the lifetime numbers that used to be four lines
    /// on the main-menu pilot card. Its own scene, reached from the menu's meta row beside
    /// the Shop and Hangar, so the menu slot could go to the leaderboard.
    ///
    /// <para>Read-only, and holds no state of its own (§1): every value is pulled straight
    /// from <see cref="ProfileService"/> and <see cref="AchievementService"/> on enable and
    /// whenever either raises <c>Changed</c>. Prefab-style — every label is an Inspector
    /// reference wired by <c>StatsBuilder</c> — so this class only formats and assigns text.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class StatsScreen : MonoBehaviour
    {
        [Header("Runs")]
        [SerializeField] private Text _runsValue;
        [SerializeField] private Text _bestTimeValue;
        [SerializeField] private Text _bestLevelValue;
        [SerializeField] private Text _bestKillsValue;

        [Header("Lifetime")]
        [SerializeField] private Text _lifetimeKillsValue;
        [SerializeField] private Text _bossKillsValue;
        [SerializeField] private Text _lifetimeScrapValue;

        [Header("Now")]
        [SerializeField] private Text _walletValue;
        [SerializeField] private Text _achievementsValue;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;
        [SerializeField] private string _menuSceneName = "MainMenu";

        private void Awake()
        {
            EnsureEventSystem();
            if (_backButton != null)
                _backButton.onClick.AddListener(() => SceneManager.LoadScene(_menuSceneName));
        }

        private void OnEnable()
        {
            Refresh();
            ProfileService.Changed += Refresh;
            AchievementService.Changed += Refresh;
        }

        private void OnDisable()
        {
            ProfileService.Changed -= Refresh;
            AchievementService.Changed -= Refresh;
        }

        private void Refresh()
        {
            var p = ProfileService.Current;

            Set(_runsValue, p.runsPlayed.ToString("n0"));
            Set(_bestTimeValue, Clock(p.bestSurvivalSeconds));
            Set(_bestLevelValue, p.bestLevel > 0 ? p.bestLevel.ToString() : "–");
            Set(_bestKillsValue, p.bestKills.ToString("n0"));

            Set(_lifetimeKillsValue, p.lifetimeKills.ToString("n0"));
            Set(_bossKillsValue, p.bossKills.ToString("n0"));
            Set(_lifetimeScrapValue, p.lifetimeScrap.ToString("n0"));

            Set(_walletValue, p.wallet.ToString("n0"));
            Set(_achievementsValue, Achievements());
        }

        private static string Achievements()
        {
            int done = 0, total = 0;
            foreach (var a in AchievementService.All)
            {
                total++;
                if (AchievementService.IsUnlocked(a)) done++;
            }
            return total > 0 ? $"{done} / {total}" : "–";
        }

        private static void Set(Text label, string value)
        {
            if (label != null) label.text = value;
        }

        private static string Clock(int seconds)
        {
            int s = Mathf.Max(0, seconds);
            return $"{s / 60:00}:{s % 60:00}";
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
