using System;
using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// The single place the game touches the persistent <see cref="PlayerProfile"/>. Static,
    /// like <see cref="SettingsService"/> / <see cref="HighScoreService"/> — every currency
    /// mutation funnels through <see cref="AddScrap"/> / <see cref="TrySpend"/> so a future
    /// server-authoritative backend has one seam to intercept (Project_Goals §8).
    ///
    /// The backing <see cref="IProfileStore"/> is swappable: local JSON by default, an HTTP
    /// store later, a fake in tests. The live profile is held in memory for the session and
    /// written on <see cref="Save"/> (the run-end screen calls it).
    /// </summary>
    public static class ProfileService
    {
        private static IProfileStore _store;
        private static PlayerProfile _current;

        /// <summary>Raised after any change to the live profile (wallet, run history, …).</summary>
        public static event Action Changed;

        /// <summary>The live profile for this session. Loaded on first access.</summary>
        public static PlayerProfile Current
        {
            get { EnsureLoaded(); return _current; }
        }

        public static long Wallet => Current.wallet;
        public static long LifetimeScrap => Current.lifetimeScrap;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot() => EnsureLoaded();

        private static void EnsureLoaded()
        {
            _store ??= new LocalJsonProfileStore();
            _current ??= _store.Load();
        }

        /// <summary>Swap the persistence layer (tests, or a future backend). Reloads at once.</summary>
        public static void SetStore(IProfileStore store)
        {
            _store = store;
            _current = store?.Load();
            Changed?.Invoke();
        }

        /// <summary>Credit scrap carried out of a run. Non-positive amounts are ignored.</summary>
        public static void AddScrap(long amount)
        {
            if (amount <= 0) return;
            EnsureLoaded();
            _current.wallet += amount;
            _current.lifetimeScrap += amount;
            Changed?.Invoke();
        }

        /// <summary>Spend from the wallet. Returns false and changes nothing if the balance is short.</summary>
        public static bool TrySpend(long amount)
        {
            if (amount <= 0) return true;
            EnsureLoaded();
            if (_current.wallet < amount) return false;
            _current.wallet -= amount;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Record that a run finished, for lifetime history stats.</summary>
        public static void RecordRun(int kills)
        {
            EnsureLoaded();
            _current.runsPlayed++;
            if (kills > _current.bestKills) _current.bestKills = kills;
            Changed?.Invoke();
        }

        /// <summary>Persist the live profile through the store.</summary>
        public static void Save()
        {
            EnsureLoaded();
            _store.Save(_current);
        }
    }
}
