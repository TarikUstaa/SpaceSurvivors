using System;
using System.Collections.Generic;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Persistence seam for per-mode best results, mirroring <see cref="IProfileStore"/>.
    /// M13 ships <see cref="LocalPrefsLeaderboardStore"/> (PlayerPrefs, this device only); an
    /// online leaderboard drops in later behind this same interface so
    /// <see cref="HighScoreService"/> and its callers never change (Project_Goals §8: online
    /// leaderboards for Infinite mode are a planned backend feature).
    ///
    /// Like <see cref="IProfileStore"/> the contract is synchronous / cache-first: a networked
    /// store answers <see cref="BestSeconds"/> from its last known value and reconciles in the
    /// background.
    /// </summary>
    public interface ILeaderboardStore
    {
        /// <summary>This player's best survival time (seconds) for the mode, 0 if none.</summary>
        float BestSeconds(string modeId);

        /// <summary>
        /// Submit a finished run. Returns true if it beat this player's stored best — the end
        /// screen shows "NEW BEST" off this, so the answer must come back immediately even when
        /// a remote store is still pushing in the background.
        ///
        /// <para>The whole <see cref="RunResult"/> is passed, not just the time: the local store
        /// only ranks on seconds, but an online board also displays kills and level, and the
        /// server uses them to sanity-check the run.</para>
        /// </summary>
        bool Submit(string modeId, RunResult run);

        /// <summary>
        /// Fetch the ranked board for a mode. Unlike the rest of this interface there is no
        /// synchronous answer to give — a board is other players' data, which only the server
        /// has — so the result arrives through <paramref name="onDone"/> instead.
        ///
        /// <para><paramref name="onDone"/> is always called exactly once, on the main thread,
        /// and never with null: a store with nothing to show returns an empty
        /// <see cref="LeaderboardBoard"/> with <see cref="LeaderboardBoard.FromServer"/> false,
        /// so the screen can say "offline" rather than hang on a spinner.</para>
        /// </summary>
        void FetchBoard(string modeId, int limit, Action<LeaderboardBoard> onDone);
    }

    /// <summary>The ranked board for one mode, plus where this player sits in it.</summary>
    public sealed class LeaderboardBoard
    {
        /// <summary>The game's mode id this board is for (e.g. <c>Mode_Infinite</c>).</summary>
        public string ModeId;

        /// <summary>
        /// True when the rows came from the server. False means the network was unreachable
        /// or the backend is switched off, and <see cref="Entries"/> is at most this device's
        /// own best — the screen shows that plainly rather than pretending it is a ranking.
        /// </summary>
        public bool FromServer;

        /// <summary>Top rows, best first. Never null; may be empty.</summary>
        public IReadOnlyList<LeaderboardRow> Entries = Array.Empty<LeaderboardRow>();

        /// <summary>
        /// This player's own standing, even when they fall outside <see cref="Entries"/>.
        /// Null when they have no ranked run in this mode yet.
        /// </summary>
        public LeaderboardRow Me;

        public static LeaderboardBoard Offline(string modeId, LeaderboardRow me = null) => new()
        {
            ModeId = modeId,
            FromServer = false,
            Entries = me != null ? new[] { me } : Array.Empty<LeaderboardRow>(),
            Me = me,
        };
    }

    /// <summary>One line of a <see cref="LeaderboardBoard"/>.</summary>
    public sealed class LeaderboardRow
    {
        /// <summary>1-based position. 0 when unknown (a local-only "your best" row).</summary>
        public int Rank;
        public string Name = "";
        public float Seconds;
        public int Kills;
        public int Level;

        /// <summary>Set by the store so the screen can highlight this player's row.</summary>
        public bool IsMe;
    }

    /// <summary>Default <see cref="ILeaderboardStore"/>: per-mode bests in <see cref="UnityEngine.PlayerPrefs"/>.</summary>
    public sealed class LocalPrefsLeaderboardStore : ILeaderboardStore
    {
        private static string Key(string modeId)
            => $"score.best.{(string.IsNullOrEmpty(modeId) ? "default" : modeId)}";

        public float BestSeconds(string modeId) => UnityEngine.PlayerPrefs.GetFloat(Key(modeId), 0f);

        public bool Submit(string modeId, RunResult run)
        {
            string key = Key(modeId);
            if (run.SurvivedSeconds <= UnityEngine.PlayerPrefs.GetFloat(key, 0f)) return false;
            UnityEngine.PlayerPrefs.SetFloat(key, run.SurvivedSeconds);
            UnityEngine.PlayerPrefs.Save();
            return true;
        }

        /// <summary>
        /// There is no board to fetch without a server — this store only ever knew this
        /// device. It answers with the one row it has (this player's best) so the screen has
        /// something honest to show, marked <see cref="LeaderboardBoard.FromServer"/> false.
        /// </summary>
        public void FetchBoard(string modeId, int limit, Action<LeaderboardBoard> onDone)
        {
            float best = BestSeconds(modeId);
            LeaderboardRow me = best > 0f
                ? new LeaderboardRow { Rank = 0, Name = "YOU", Seconds = best, IsMe = true }
                : null;
            onDone(LeaderboardBoard.Offline(modeId, me));
        }
    }
}
