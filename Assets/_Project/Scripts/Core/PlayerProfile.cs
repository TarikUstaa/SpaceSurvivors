using System;
using System.Collections.Generic;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// The persistent player record — everything that must survive between runs. A plain
    /// serializable data bag with a <see cref="schemaVersion"/> so old saves can be migrated
    /// (AI_Guidelines §3, and Project_Goals §8: this DTO is what a future backend stores, so
    /// it stays framework-free — no UnityEngine types, round-trips through any JSON lib).
    ///
    /// M13 only uses <see cref="wallet"/> / run history. The M14 fields (owned upgrades,
    /// ships, achievements) are declared now so adding those features doesn't bump the schema.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public const int CurrentSchemaVersion = 4;

        public int schemaVersion = CurrentSchemaVersion;

        // ---- Currency ----
        /// <summary>Spendable balance (scrap carried out of runs).</summary>
        public long wallet;
        /// <summary>Total scrap ever earned — a lifetime stat, never decremented.</summary>
        public long lifetimeScrap;

        // ---- Run history / lifetime stats (feed M14c achievements) ----
        public int runsPlayed;
        public int bestKills;
        /// <summary>Enemies destroyed across every run.</summary>
        public long lifetimeKills;
        /// <summary>Longest single run, whole seconds.</summary>
        public int bestSurvivalSeconds;
        /// <summary>Highest level reached in any run.</summary>
        public int bestLevel;
        /// <summary>Scheduled bosses defeated across every run.</summary>
        public int bossKills;

        // ---- Meta progression (M14a): permanent stat upgrades bought with wallet scrap ----
        /// <summary>Owned level per meta-upgrade id. Absent id = level 0.</summary>
        public Dictionary<string, int> metaUpgradeLevels = new();

        // ---- Reserved for M14b/M14c (declared early to keep the schema stable) ----
        public List<string> ownedShipIds = new();
        public string selectedShipId = "";
        public List<string> unlockedAchievementIds = new();

        // ---- Maps (M15): all maps are free, this is just the last pick ----
        public string selectedMapId = "";
    }
}
