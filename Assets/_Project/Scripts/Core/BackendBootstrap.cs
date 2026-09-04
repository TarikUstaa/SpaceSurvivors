using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Installs the networked stores at startup — the one place the game decides whether it is
    /// talking to a server at all.
    ///
    /// <para>Runs before the first scene loads so the store is in place prior to any scene
    /// object reading the profile — <see cref="ProfileService.SetStore"/> reloads through the
    /// new store, and anything holding the previously loaded instance would be stranded on it.
    /// Order against <see cref="ProfileService"/>'s own boot hook does not matter: whichever
    /// runs first, the other one finds its work already done.</para>
    ///
    /// <para>If <see cref="BackendConfig.Enabled"/> is off (the default) nothing happens and the
    /// game stays purely local.</para>
    /// </summary>
    public static class BackendBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (!BackendConfig.Enabled) return;

            var userId = BackendConfig.UserId;
            var cache = new LocalJsonProfileStore(BackendConfig.CacheFileName);
            ProfileService.SetStore(new HttpProfileStore(BackendConfig.ProfileUrl, userId, cache));

            Debug.Log($"[Backend] cloud sync on — {BackendConfig.BaseUrl} as '{userId}'"
                      + (BackendConfig.Sandbox ? "  [SANDBOX cache]" : ""));
        }
    }
}
