using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Installs the networked stores — the one place the game decides whether it is talking to a
    /// server at all.
    ///
    /// <para><see cref="Install"/> runs before the first scene loads so the store is in place
    /// prior to any scene object reading the profile — <see cref="ProfileService.SetStore"/>
    /// reloads through the new store, and anything holding the previously loaded instance would
    /// be stranded on it. Order against <see cref="ProfileService"/>'s own boot hook does not
    /// matter: whichever runs first, the other one finds its work already done.</para>
    ///
    /// <para>If <see cref="BackendConfig.Enabled"/> is off (the default) the game stays purely
    /// local.</para>
    /// </summary>
    public static class BackendBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install() => Apply();

        /// <summary>
        /// Put the stores in whatever state <see cref="BackendConfig.Enabled"/> currently asks
        /// for. Startup calls it, and so does the settings toggle — which is the reason it exists
        /// as a separate method: a player who turns cloud sync on should see it happen, not be
        /// told to restart the game.
        ///
        /// <para>Switching either way is safe mid-session. <see cref="ProfileService.SetStore"/>
        /// detaches the old store, reloads through the new one and announces the change, and both
        /// stores read the same file on disk — the cloud store keeps a local cache and that cache
        /// <em>is</em> the ordinary save (except in sandbox mode, which points it at a throwaway
        /// file on purpose). So nothing is lost by turning sync off, and turning it on starts from
        /// the progress already there rather than from nothing.</para>
        /// </summary>
        public static void Apply()
        {
            if (BackendConfig.Enabled)
            {
                var cache = new LocalJsonProfileStore(BackendConfig.CacheFileName);
                ProfileService.SetStore(new HttpProfileStore(BackendConfig.ProgressUrl, cache));
                HighScoreService.SetStore(new HttpLeaderboardStore(BackendConfig.LeaderboardUrl));

                Debug.Log($"[Backend] cloud sync on — {BackendConfig.BaseUrl} as '{BackendConfig.UserId}'"
                          + (BackendConfig.Sandbox ? "  [SANDBOX cache]" : ""));
            }
            else
            {
                ProfileService.SetStore(new LocalJsonProfileStore(BackendConfig.CacheFileName));
                HighScoreService.SetStore(new LocalPrefsLeaderboardStore());

                Debug.Log("[Backend] cloud sync off — playing locally.");
            }
        }
    }
}
