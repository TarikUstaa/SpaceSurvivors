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
    /// The permanent-upgrade shop (M14a). One row per <c>MetaUpgradeData</c> in the
    /// catalogue; BUY spends wallet scrap via <see cref="MetaProgressionService"/>. Prefab-
    /// style — every widget is an Inspector reference, wired by <c>ShopBuilder</c>
    /// (AI_Guidelines §7). It only reads services and pushes text; no game logic here.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShopScreen : MonoBehaviour
    {
        [Serializable]
        public struct UpgradeRow
        {
            public string upgradeId;
            public Image icon;
            public Text title;
            public Text description;
            public Text levelLabel;
            public Text costLabel;
            public Button buyButton;
        }

        [SerializeField] private Text _walletLabel;
        [SerializeField] private UpgradeRow[] _rows = Array.Empty<UpgradeRow>();
        [SerializeField] private Button _backButton;
        [SerializeField] private string _menuSceneName = "MainMenu";

        private void Awake()
        {
            EnsureEventSystem();

            foreach (var row in _rows)
            {
                if (row.buyButton == null) continue;
                string id = row.upgradeId;
                row.buyButton.onClick.AddListener(() => Buy(id));
            }

            if (_backButton != null)
                _backButton.onClick.AddListener(() => SceneManager.LoadScene(_menuSceneName));

            MetaProgressionService.Changed += RefreshAll;
            ProfileService.Changed += RefreshAll;
        }

        private void OnDestroy()
        {
            MetaProgressionService.Changed -= RefreshAll;
            ProfileService.Changed -= RefreshAll;
        }

        private void Start() => RefreshAll();

        private void Buy(string id)
        {
            var u = MetaProgressionService.Find(id);
            MetaProgressionService.TryPurchase(u); // Changed -> RefreshAll
        }

        private void RefreshAll()
        {
            if (_walletLabel != null)
                _walletLabel.text = $"SCRAP  {ProfileService.Wallet:n0}";

            foreach (var row in _rows)
            {
                var u = MetaProgressionService.Find(row.upgradeId);
                if (u == null) continue;

                if (row.icon != null && u.icon != null) row.icon.sprite = u.icon;
                if (row.title != null) row.title.text = u.title;
                if (row.description != null) row.description.text = u.description;

                int lvl = MetaProgressionService.LevelOf(u.id);
                if (row.levelLabel != null) row.levelLabel.text = $"{lvl} / {u.maxLevel}";

                bool maxed = MetaProgressionService.IsMaxed(u);
                if (row.costLabel != null)
                    row.costLabel.text = maxed ? "MAX" : $"{MetaProgressionService.CostToNext(u):n0}";
                if (row.buyButton != null)
                    row.buyButton.interactable = !maxed && MetaProgressionService.CanAfford(u);
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
