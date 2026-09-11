using System;
using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// The achievements layer (M14c). Reads the <see cref="AchievementCatalogue"/> (Resources),
    /// resolves each achievement's watched metric to a single lifetime number, and records
    /// unlocks into <c>PlayerProfile.unlockedAchievementIds</c> through <see cref="ProfileService"/>.
    ///
    /// Static like <see cref="MetaProgressionService"/> / <see cref="ShipService"/>. There is no
    /// per-achievement code: <see cref="Evaluate"/> is "for each: value &gt;= target ? unlock".
    /// Call <see cref="Evaluate"/> when the lifetime stats change (the run-end screen does).
    /// </summary>
    public static class AchievementService
    {
        private const string CatalogueResource = "AchievementCatalogue";

        private static AchievementCatalogue _catalogue;
        private static readonly List<AchievementData> _empty = new();

        /// <summary>Raised after <see cref="Evaluate"/> unlocks one or more achievements.</summary>
        public static event Action Changed;

        /// <summary>
        /// Auto-track: any profile mutation (scrap banked, run recorded, ship or upgrade
        /// bought) re-checks every achievement. <see cref="ProfileService.Changed"/> fires only
        /// at run end and on shop purchases — never per-frame — so this is cheap. Callers can
        /// still invoke <see cref="Evaluate"/> directly (the run-end screen does, to read back
        /// what was unlocked).
        ///
        /// <para><b>Detach before attaching, and a named method rather than a lambda.</b> This
        /// project runs with Domain Reload disabled (Editor → Enter Play Mode Options), so
        /// statics survive between Play sessions while this hook runs again on every one. A
        /// lambda cannot be removed — nothing holds a reference to it — so the old subscription
        /// stayed attached and a fifth Play press meant every profile change re-evaluated every
        /// achievement five times. The <c>-=</c> is a no-op the first time round, which is what
        /// makes the pair safe to run repeatedly. <c>GameSession</c> and <c>BossMarker</c> guard
        /// the same hazard with a <c>SubsystemRegistration</c> reset; this was the one place the
        /// pattern had not reached.</para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            ProfileService.Changed -= OnProfileChanged;
            ProfileService.Changed += OnProfileChanged;
        }

        private static void OnProfileChanged() => Evaluate();

        public static IReadOnlyList<AchievementData> All
        {
            get { EnsureLoaded(); return _catalogue != null ? _catalogue.achievements : _empty; }
        }

        /// <summary>Test / editor hook: inject a catalogue instead of loading from Resources.</summary>
        public static void SetCatalogue(AchievementCatalogue catalogue) => _catalogue = catalogue;

        private static void EnsureLoaded()
        {
            if (_catalogue == null)
                _catalogue = Resources.Load<AchievementCatalogue>(CatalogueResource);
            if (_catalogue == null)
                Debug.LogWarning($"[Achievements] no '{CatalogueResource}' in a Resources folder — screen will be empty.");
        }

        public static AchievementData Find(string id)
        {
            EnsureLoaded();
            if (_catalogue == null) return null;
            foreach (var a in _catalogue.achievements)
                if (a != null && a.id == id) return a;
            return null;
        }

        /// <summary>Current value of the metric this achievement watches.</summary>
        public static long Value(AchievementData a)
        {
            if (a == null) return 0;
            var p = ProfileService.Current;
            return a.metric switch
            {
                AchievementMetric.LifetimeKills => p.lifetimeKills,
                AchievementMetric.BestKillsInRun => p.bestKills,
                AchievementMetric.BestSurvivalSeconds => p.bestSurvivalSeconds,
                AchievementMetric.RunsPlayed => p.runsPlayed,
                AchievementMetric.LifetimeScrap => p.lifetimeScrap,
                AchievementMetric.BossKills => p.bossKills,
                AchievementMetric.BestLevel => p.bestLevel,
                AchievementMetric.ShipsOwned => CountOwnedShips(),
                AchievementMetric.MetaUpgradeLevels => SumMetaUpgradeLevels(),
                _ => 0,
            };
        }

        /// <summary>The value the metric must reach. Usually <c>a.threshold</c>; for
        /// "Ships Owned" with threshold 0 it resolves to the whole catalogue.</summary>
        public static long Target(AchievementData a)
        {
            if (a == null) return 0;
            if (a.metric == AchievementMetric.ShipsOwned && a.threshold <= 0)
                return Mathf.Max(1, ShipService.Ships.Count);
            return Math.Max(1, a.threshold);
        }

        public static float Progress01(AchievementData a)
        {
            long target = Target(a);
            return target <= 0 ? 1f : Mathf.Clamp01((float)Value(a) / target);
        }

        /// <summary>True if this achievement has been earned — either already recorded in the
        /// profile, or its condition is met right now.</summary>
        public static bool IsUnlocked(AchievementData a)
        {
            if (a == null) return false;
            if (ProfileService.Current.unlockedAchievementIds.Contains(a.id)) return true;
            return Value(a) >= Target(a);
        }

        /// <summary>
        /// Check every achievement, record any newly-earned ones on the profile, save once,
        /// and raise <see cref="Changed"/> if anything changed. Returns the achievements that
        /// were unlocked by this call (for a toast, if the caller wants one).
        /// </summary>
        public static List<AchievementData> Evaluate()
        {
            EnsureLoaded();
            var unlocked = new List<AchievementData>();
            if (_catalogue == null) return unlocked;

            var owned = ProfileService.Current.unlockedAchievementIds;
            foreach (var a in _catalogue.achievements)
            {
                if (a == null || owned.Contains(a.id)) continue;
                if (Value(a) < Target(a)) continue;
                owned.Add(a.id);
                unlocked.Add(a);
            }

            if (unlocked.Count > 0)
            {
                ProfileService.Save();
                Changed?.Invoke();
            }
            return unlocked;
        }

        private static long CountOwnedShips()
        {
            long n = 0;
            foreach (var s in ShipService.Ships)
                if (s != null && ShipService.IsOwned(s.id)) n++;
            return n;
        }

        private static long SumMetaUpgradeLevels()
        {
            long n = 0;
            foreach (var kv in ProfileService.Current.metaUpgradeLevels)
                n += kv.Value;
            return n;
        }
    }
}
