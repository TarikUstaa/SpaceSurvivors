using System;
using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// The permanent-upgrade layer (M14a). Reads the <see cref="MetaUpgradeCatalogue"/>
    /// (Resources), answers "what level am I / what does the next one cost / can I buy it",
    /// and builds the run-start <see cref="StatModifier"/> list. Purchases funnel through
    /// <see cref="ProfileService"/> so the wallet stays the single seam (Project_Goals §8).
    ///
    /// Static like <see cref="ProfileService"/> / <see cref="SettingsService"/>: the catalogue
    /// is one small config asset that both the shop scene and the game scene need.
    /// </summary>
    public static class MetaProgressionService
    {
        private const string CatalogueResource = "MetaUpgradeCatalogue";

        private static MetaUpgradeCatalogue _catalogue;
        private static readonly List<MetaUpgradeData> _empty = new();

        /// <summary>Raised after a successful purchase.</summary>
        public static event Action Changed;

        public static IReadOnlyList<MetaUpgradeData> Upgrades
        {
            get { EnsureLoaded(); return _catalogue != null ? _catalogue.upgrades : _empty; }
        }

        /// <summary>Test / editor hook: inject a catalogue instead of loading from Resources.</summary>
        public static void SetCatalogue(MetaUpgradeCatalogue catalogue) => _catalogue = catalogue;

        private static void EnsureLoaded()
        {
            if (_catalogue == null)
                _catalogue = Resources.Load<MetaUpgradeCatalogue>(CatalogueResource);
            if (_catalogue == null)
                Debug.LogWarning($"[Meta] no '{CatalogueResource}' in a Resources folder — shop will be empty.");
        }

        public static MetaUpgradeData Find(string id)
        {
            EnsureLoaded();
            if (_catalogue == null) return null;
            foreach (var u in _catalogue.upgrades)
                if (u != null && u.id == id) return u;
            return null;
        }

        public static int LevelOf(string id)
            => ProfileService.Current.metaUpgradeLevels.TryGetValue(id, out int lvl) ? lvl : 0;

        public static bool IsMaxed(MetaUpgradeData u) => u != null && LevelOf(u.id) >= u.maxLevel;

        /// <summary>Scrap cost of the next level, or -1 if already maxed.</summary>
        public static long CostToNext(MetaUpgradeData u)
        {
            if (u == null) return -1;
            int lvl = LevelOf(u.id);
            return lvl >= u.maxLevel ? -1 : u.CostForNext(lvl);
        }

        public static bool CanAfford(MetaUpgradeData u)
        {
            long cost = CostToNext(u);
            return cost >= 0 && ProfileService.Wallet >= cost;
        }

        /// <summary>
        /// Buy one level of <paramref name="u"/>. Deducts scrap, bumps the profile level,
        /// saves. Returns false (no change) if maxed or short on scrap.
        /// </summary>
        public static bool TryPurchase(MetaUpgradeData u)
        {
            if (u == null) return false;
            int lvl = LevelOf(u.id);
            if (lvl >= u.maxLevel) return false;

            long cost = u.CostForNext(lvl);
            if (!ProfileService.TrySpend(cost)) return false;

            ProfileService.Current.metaUpgradeLevels[u.id] = lvl + 1;
            ProfileService.Save();
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// The permanent modifiers to push onto the player's <see cref="StatSheet"/> at the
        /// start of a run — one <see cref="StatModifier"/> per owned upgrade, scaled by level.
        /// </summary>
        public static List<StatModifier> BuildStartingModifiers()
        {
            EnsureLoaded();
            var list = new List<StatModifier>();
            if (_catalogue == null) return list;

            foreach (var u in _catalogue.upgrades)
            {
                if (u == null) continue;
                int lvl = LevelOf(u.id);
                // One copy of perLevel per owned level — the StatSheet accumulates them, so
                // this is correct for Flat / PercentAdd / Multiplier alike.
                for (int i = 0; i < lvl; i++)
                    list.Add(u.perLevel);
            }
            return list;
        }
    }
}
