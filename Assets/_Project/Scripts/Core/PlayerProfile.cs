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
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;

        // ---- Currency ----
        /// <summary>Spendable balance (scrap carried out of runs).</summary>
        public long wallet;
        /// <summary>Total scrap ever earned — a lifetime stat, never decremented.</summary>
        public long lifetimeScrap;

        // ---- Run history ----
        public int runsPlayed;
        public int bestKills;

        // ---- Reserved for M14 meta screens (declared early to keep the schema stable) ----
        public List<string> ownedUpgradeIds = new();
        public List<string> ownedShipIds = new();
        public string selectedShipId = "";
        public List<string> unlockedAchievementIds = new();
    }
}
