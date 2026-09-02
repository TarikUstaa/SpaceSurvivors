using System;
using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// The single place the game touches the persistent <see cref="PlayerProfile"/>. Static,
    /// like <see cref="SettingsService"/> / <see cref="HighScoreService"/> — every currency
    /// mutation funnels through <see cref="AddScrap"/> / <see cref="TrySpend"/> /
    /// <see cref="TryPurchase"/> so a future server-authoritative backend has one seam to
    /// intercept (Project_Goals §8).
    ///
    /// The backing <see cref="IProfileStore"/> is swappable: local JSON by default, an HTTP
    /// store later (cache-first — see <see cref="IRemoteProfileStore"/>), a fake in tests. The
    /// live profile is held in memory for the session and written on <see cref="Save"/>.
    /// </summary>
    public static class ProfileService
    {
        private static IProfileStore _store;
        private static PlayerProfile _current;

        /// <summary>Raised after any change to the live profile (wallet, run history, …).</summary>
        public static event Action Changed;

        /// <summary>
        /// Raised when the backing store reports a change in sync health (only a remote store
        /// does — a local store is always <see cref="ProfileSyncStatus.Synced"/>). UI can watch
        /// this to show an offline / syncing indicator.
        /// </summary>
        public static event Action<ProfileSyncStatus> SyncStatusChanged;

        /// <summary>The live profile for this session. Loaded on first access.</summary>
        public static PlayerProfile Current
        {
            get { EnsureLoaded(); return _current; }
        }

        public static long Wallet => Current.wallet;
        public static long LifetimeScrap => Current.lifetimeScrap;

        /// <summary>The signed-in account id, or "" for local / anonymous play.</summary>
        public static string UserId => Current.userId ?? "";

        /// <summary>Current sync health of the backing store (always Synced for a local store).</summary>
        public static ProfileSyncStatus SyncStatus =>
            _store is IRemoteProfileStore r ? r.SyncStatus : ProfileSyncStatus.Synced;

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
            if (_store is IRemoteProfileStore oldRemote)
                oldRemote.SyncStatusChanged -= RaiseSyncStatus;

            _store = store;
            _current = store?.Load();

            if (_store is IRemoteProfileStore newRemote)
                newRemote.SyncStatusChanged += RaiseSyncStatus;

            Changed?.Invoke();
            RaiseSyncStatus(SyncStatus);
        }

        private static void RaiseSyncStatus(ProfileSyncStatus status) => SyncStatusChanged?.Invoke(status);

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

        /// <summary>
        /// One atomic "buy": debit <paramref name="cost"/> and, only if that succeeds, run
        /// <paramref name="grant"/> to apply what was bought, then persist once. Returns false
        /// (touching nothing) if the wallet is short. This is the single seam a server-
        /// authoritative backend intercepts — spend + grant must not be two independent
        /// client writes (Project_Goals §8).
        /// </summary>
        public static bool TryPurchase(long cost, Action grant)
        {
            EnsureLoaded();
            if (cost > 0 && _current.wallet < cost) return false;

            if (cost > 0) _current.wallet -= cost;
            grant?.Invoke();

            Changed?.Invoke();
            Save();
            return true;
        }

        /// <summary>Record that a run finished, folding its figures into the lifetime stats
        /// that drive achievements (M14c). Called once per run by the end screen.</summary>
        public static void RecordRun(int kills, int level, float survivedSeconds, int bossesDefeated)
        {
            EnsureLoaded();
            _current.runsPlayed++;
            _current.lifetimeKills += Math.Max(0, kills);
            _current.bossKills += Math.Max(0, bossesDefeated);
            if (kills > _current.bestKills) _current.bestKills = kills;
            if (level > _current.bestLevel) _current.bestLevel = level;
            int secs = Math.Max(0, (int)survivedSeconds);
            if (secs > _current.bestSurvivalSeconds) _current.bestSurvivalSeconds = secs;
            Changed?.Invoke();
        }

        /// <summary>
        /// Persist the live profile through the store. Fire-and-forget: a remote store writes
        /// its local cache synchronously and syncs in the background, reporting failures via
        /// <see cref="SyncStatusChanged"/> — a save never throws into gameplay.
        /// </summary>
        public static void Save()
        {
            EnsureLoaded();
            try
            {
                _store.Save(_current);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Profile] Save failed: {e}");
            }
        }
    }
}
