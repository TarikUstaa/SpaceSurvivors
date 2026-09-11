using System;
using SpaceSurvivors.Core;
using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// The achievements screen (M14c). One tile per <c>AchievementData</c> in the catalogue;
    /// a tile shows its icon, name, description and either "✓ UNLOCKED" or a "value / target"
    /// progress line. Prefab-style — every widget is an Inspector reference wired by
    /// <c>AchievementsBuilder</c> (AI_Guidelines §7). It only reads <see cref="AchievementService"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class AchievementsScreen : MonoBehaviour
    {
        [Serializable]
        public struct Tile
        {
            public string achievementId;
            public Image icon;
            public Text title;
            public Text description;
            public Text progress;
            [Tooltip("Optional: a badge shown only when the achievement is unlocked.")]
            public GameObject unlockedBadge;
            [Tooltip("Optional: the tile background, dimmed while locked.")]
            public Image background;
        }

        [SerializeField] private Text _summaryLabel;
        [SerializeField] private Tile[] _tiles = Array.Empty<Tile>();
        [SerializeField] private Button _backButton;
        [SerializeField] private string _menuSceneName = "MainMenu";

        [Header("Tile tint")]
        [SerializeField] private Color _lockedTint = new(0.40f, 0.43f, 0.50f, 1f);
        [SerializeField] private Color _unlockedTint = new(0.72f, 0.77f, 0.86f, 1f);
        [SerializeField] private Color _lockedText = new(0.60f, 0.64f, 0.72f, 1f);
        [SerializeField] private Color _unlockedTitle = new(1f, 0.95f, 0.80f, 1f);
        [SerializeField] private Color _unlockedStamp = new(0.44f, 0.90f, 0.55f, 1f);

        private void Awake()
        {
            EnsureEventSystem();

            if (_backButton != null)
                _backButton.onClick.AddListener(() => SceneManager.LoadScene(_menuSceneName));

            AchievementService.Changed += Refresh;
            ProfileService.Changed += Refresh;
        }

        private void OnDestroy()
        {
            AchievementService.Changed -= Refresh;
            ProfileService.Changed -= Refresh;
        }

        private void Start()
        {
            // A stat could have crossed a threshold outside this screen (e.g. buying the last
            // ship in the hangar); make sure the profile is current before we draw.
            AchievementService.Evaluate();
            Refresh();
        }

        private void Refresh()
        {
            int unlockedCount = 0, total = 0;

            foreach (var tile in _tiles)
            {
                var a = AchievementService.Find(tile.achievementId);
                if (a == null) continue;
                total++;

                bool unlocked = AchievementService.IsUnlocked(a);
                if (unlocked) unlockedCount++;

                if (tile.icon != null)
                {
                    if (a.icon != null) tile.icon.sprite = a.icon;
                    tile.icon.enabled = a.icon != null;
                    tile.icon.color = unlocked ? _unlockedTint : _lockedTint;
                }
                if (tile.background != null)
                    tile.background.color = unlocked ? _unlockedTint : _lockedTint;
                if (tile.title != null)
                {
                    tile.title.text = a.title;
                    tile.title.color = unlocked ? _unlockedTitle : _lockedText;
                }
                if (tile.description != null)
                {
                    tile.description.text = a.description;
                    tile.description.color = _lockedText;
                }
                if (tile.unlockedBadge != null) tile.unlockedBadge.SetActive(unlocked);

                if (tile.progress != null)
                {
                    tile.progress.text = unlocked
                        ? "✓ UNLOCKED"
                        : $"{Format(a.metric, AchievementService.Value(a))} / {Format(a.metric, AchievementService.Target(a))}";
                    tile.progress.color = unlocked ? _unlockedStamp : _lockedText;
                }
            }

            if (_summaryLabel != null)
                _summaryLabel.text = $"{unlockedCount} / {total} UNLOCKED";
        }

        /// <summary>Survival metrics read nicer as m:ss; everything else is a plain count.</summary>
        private static string Format(SpaceSurvivors.Data.AchievementMetric metric, long value)
        {
            if (metric == SpaceSurvivors.Data.AchievementMetric.BestSurvivalSeconds)
            {
                long t = Math.Max(0, value);
                return $"{t / 60}:{t % 60:00}";
            }
            return value.ToString("n0");
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
