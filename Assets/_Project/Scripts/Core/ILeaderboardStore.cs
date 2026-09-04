using System;

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
    }
}
