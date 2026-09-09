using System;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Per-mode best results. A thin static front over a swappable <see cref="ILeaderboardStore"/>
    /// (see <see cref="SettingsService"/> for the static-wrapper rationale) — local PlayerPrefs
    /// by default, an online leaderboard later via <see cref="SetStore"/>, a fake in tests.
    /// Keyed by an arbitrary mode id string so Campaign and Infinite keep separate bests.
    ///
    /// Every write funnels through <see cref="Submit"/>, the one seam a leaderboard push
    /// intercepts (Project_Goals §8).
    /// </summary>
    public static class HighScoreService
    {
        private static ILeaderboardStore _store;

        /// <summary>Raised after a successful <see cref="Submit"/> (new personal best).</summary>
        public static event Action<string, float> RecordSet;

        private static ILeaderboardStore Store => _store ??= new LocalPrefsLeaderboardStore();

        /// <summary>Swap the backing store (tests, or the online leaderboard).</summary>
        public static void SetStore(ILeaderboardStore store) => _store = store;

        /// <summary>Best survival time (seconds) recorded for this mode, 0 if none.</summary>
        public static float BestSeconds(string modeId) => Store.BestSeconds(modeId);

        /// <summary>Record a run result. Returns true if it beat the stored best (new record).</summary>
        public static bool Submit(string modeId, RunResult run)
        {
            bool record = Store.Submit(modeId, run);
            if (record) RecordSet?.Invoke(modeId, run.SurvivedSeconds);
            return record;
        }

        /// <summary>
        /// Fetch the ranked board for a mode. The callback runs once on the main thread and
        /// is never handed null — an offline store answers with this device's own best,
        /// flagged <see cref="LeaderboardBoard.FromServer"/> false. See
        /// <see cref="ILeaderboardStore.FetchBoard"/>.
        /// </summary>
        public static void FetchBoard(string modeId, int limit, Action<LeaderboardBoard> onDone)
            => Store.FetchBoard(modeId, limit, onDone);
    }
}
