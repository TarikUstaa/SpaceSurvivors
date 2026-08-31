using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// What lifetime figure an achievement watches. Every metric resolves to a single
    /// non-decreasing number read from the profile (or, for <see cref="ShipsOwned"/> /
    /// <see cref="MetaUpgradeLevels"/>, from the meta services) — so an achievement is just
    /// "metric &gt;= threshold". Keeps <c>AchievementService</c> free of per-achievement code.
    /// </summary>
    public enum AchievementMetric
    {
        LifetimeKills = 0,
        BestKillsInRun = 1,
        BestSurvivalSeconds = 2,
        RunsPlayed = 3,
        LifetimeScrap = 4,
        BossKills = 5,
        BestLevel = 6,
        ShipsOwned = 7,
        MetaUpgradeLevels = 8,
    }

    /// <summary>
    /// One achievement (M14c). Pure data, like <see cref="MetaUpgradeData"/> / <see cref="ShipData"/>.
    /// Unlock state is NOT stored here — it lives in <c>PlayerProfile.unlockedAchievementIds</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Meta/Achievement", fileName = "Achievement")]
    public class AchievementData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable key stored in the profile. Never rename once shipped.")]
        public string id = "achievement";
        public string title = "Achievement";
        [TextArea] public string description = "";
        public Sprite icon;

        [Header("Unlock condition")]
        public AchievementMetric metric = AchievementMetric.LifetimeKills;
        [Tooltip("Value the metric must reach. For 'Ships Owned', 0 means \"all ships in the catalogue\".")]
        public long threshold = 1;
    }
}
