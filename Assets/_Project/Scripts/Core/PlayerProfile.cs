using System;
using System.Collections.Generic;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// The persistent player record — everything that must survive between runs. A plain
    /// serializable data bag with a <see cref="schemaVersion"/> so old saves can be migrated
    /// (AI_Guidelines §3, and Project_Goals §8).
    ///
    /// <para><b>Framework-free on purpose, and now literally so:</b> no UnityEngine types, so
    /// it round-trips through any JSON library. This class <em>is</em> the wire shape the
    /// backend stores — <see cref="HttpProfileStore"/> posts it verbatim as the
    /// <c>progress</c> blob. Adding or renaming a field here changes both the save file on
    /// disk and the API payload, so it wants <see cref="CurrentSchemaVersion"/> bumped and a
    /// case in <c>LocalJsonProfileStore.Migrate</c>.</para>
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public const int CurrentSchemaVersion = 5;

        public int schemaVersion = CurrentSchemaVersion;

        // ---- Identity ----
        /// <summary>
        /// The server-side player this profile belongs to. Empty while the game has never
        /// synced — cloud sync is off by default and purely local play never fills it in.
        ///
        /// <para>The <em>server</em> stamps its own player id into the blob on a successful
        /// save, so this is read rather than written here; <see cref="HttpProfileStore.Load"/>
        /// deliberately leaves it alone rather than guessing. Auth is out of band — the store
        /// carries a token, this is only the id.</para>
        /// </summary>
        public string userId = "";

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

        // ---- Ships (M14b) and achievements (M14c) ----
        /// <summary>Hulls the player has bought. <c>ShipService</c> owns the rules.</summary>
        public List<string> ownedShipIds = new();
        /// <summary>The hull the next run uses. Always one of <see cref="ownedShipIds"/> —
        /// <c>ProfileMerge</c> falls back to the first owned hull if a sync leaves it dangling.</summary>
        public string selectedShipId = "";
        /// <summary>Unlocked achievement ids. Earned, never spent, so a merge unions them.</summary>
        public List<string> unlockedAchievementIds = new();

        // ---- Maps (M15): all maps are free, this is just the last pick ----
        public string selectedMapId = "";
    }
}
