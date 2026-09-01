using SpaceSurvivors.Core;
using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Fills the main-menu "pilot record" panel from the persistent profile (M20): best survival
    /// time, lifetime kills, scrap wallet and achievement completion. Read-only; refreshes on
    /// <see cref="ProfileService.Changed"/> / <see cref="AchievementService.Changed"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class MenuStatsReadout : MonoBehaviour
    {
        [SerializeField] private Text _bestTimeValue;
        [SerializeField] private Text _killsValue;
        [SerializeField] private Text _scrapValue;
        [SerializeField] private Text _achievementsValue;

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

            if (_bestTimeValue != null)
            {
                int s = Mathf.Max(0, p.bestSurvivalSeconds);
                _bestTimeValue.text = $"{s / 60:00}:{s % 60:00}";
            }
            if (_killsValue != null) _killsValue.text = p.lifetimeKills.ToString("n0");
            if (_scrapValue != null) _scrapValue.text = p.wallet.ToString("n0");
            if (_achievementsValue != null)
            {
                int done = 0, total = 0;
                foreach (var a in AchievementService.All)
                {
                    total++;
                    if (AchievementService.IsUnlocked(a)) done++;
                }
                _achievementsValue.text = total > 0 ? $"{done}/{total}" : "—";
            }
        }
    }
}
