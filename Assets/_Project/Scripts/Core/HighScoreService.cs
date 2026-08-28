using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Per-mode best results, persisted to <see cref="PlayerPrefs"/>. A static PlayerPrefs
    /// wrapper (see <see cref="SettingsService"/> for the rationale). Keyed by an arbitrary
    /// mode id string so Campaign and Infinite keep separate bests.
    ///
    /// This is the local-only stand-in the backend will later shadow (Project_Goals §8) —
    /// keep writes funnelled through <see cref="Submit"/> so a future leaderboard push has a
    /// single seam.
    /// </summary>
    public static class HighScoreService
    {
        private static string Key(string modeId) => $"score.best.{modeId}";

        /// <summary>Best survival time (seconds) recorded for this mode, 0 if none.</summary>
        public static float BestSeconds(string modeId)
            => PlayerPrefs.GetFloat(Key(string.IsNullOrEmpty(modeId) ? "default" : modeId), 0f);

        /// <summary>
        /// Record a run result. Returns true if it beat the stored best (new record).
        /// </summary>
        public static bool Submit(string modeId, float survivedSeconds)
        {
            string id = string.IsNullOrEmpty(modeId) ? "default" : modeId;
            float prev = PlayerPrefs.GetFloat(Key(id), 0f);
            if (survivedSeconds <= prev) return false;

            PlayerPrefs.SetFloat(Key(id), survivedSeconds);
            PlayerPrefs.Save();
            return true;
        }
    }
}
