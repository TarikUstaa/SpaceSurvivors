using System;
using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Where the cloud-save backend lives and whether the game talks to it at all.
    ///
    /// Everything is held in <see cref="PlayerPrefs"/> rather than a ScriptableObject so it can
    /// be flipped without touching an asset (and so a build can be pointed at a different
    /// server without a rebuild). The editor menu under <c>SpaceSurvivors/Backend</c> is the
    /// intended way to change it.
    ///
    /// <para><b>Sync is off by default.</b> The real profile is a live save with hours of
    /// progress in it; nothing may reach the network until the player opts in.</para>
    /// </summary>
    public static class BackendConfig
    {
        private const string EnabledKey = "backend.enabled";
        private const string BaseUrlKey = "backend.baseUrl";
        private const string DeviceIdKey = "backend.userId";   // key kept: existing installs keep their id

        /// <summary>Local Spring Boot server. Replaced by the deployed URL later.</summary>
        public const string DefaultBaseUrl = "http://localhost:8080";

        /// <summary>Whether <see cref="BackendBootstrap"/> installs the HTTP stores at startup.</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 0) == 1;
            set { PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Server root, no trailing slash.</summary>
        public static string BaseUrl
        {
            get
            {
                var stored = PlayerPrefs.GetString(BaseUrlKey, "");
                return string.IsNullOrWhiteSpace(stored) ? DefaultBaseUrl : stored.TrimEnd('/');
            }
            set
            {
                PlayerPrefs.SetString(BaseUrlKey, (value ?? "").Trim().TrimEnd('/'));
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Who this install claims to be. Sent as <c>Authorization: Device &lt;id&gt;</c>, which the
        /// backend's dev auth filter trusts blindly — this is a stand-in, not authentication.
        /// Generated once per device so repeated launches map to the same server-side player;
        /// set it by hand to test as a second player.
        ///
        /// <para>When real auth lands this disappears: the id will come from a verified token
        /// and this property goes with it.</para>
        /// </summary>
        public static string UserId
        {
            get
            {
                var stored = PlayerPrefs.GetString(DeviceIdKey, "");
                if (!string.IsNullOrWhiteSpace(stored)) return stored;

                // First run on this device: mint a stable id and keep it.
                var generated = "dev-" + Guid.NewGuid().ToString("N");
                UserId = generated;
                return generated;
            }
            set { PlayerPrefs.SetString(DeviceIdKey, (value ?? "").Trim()); PlayerPrefs.Save(); }
        }

        /// <summary>
        /// Point the local cache at a throwaway file instead of the real <c>profile.json</c>.
        /// Testing cloud sync against a live save is how the profile got wrecked during the M17
        /// weapon pass; this makes the safe option one click away.
        /// </summary>
        public static bool Sandbox
        {
            get => PlayerPrefs.GetInt(SandboxKey, 0) == 1;
            set { PlayerPrefs.SetInt(SandboxKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        private const string SandboxKey = "backend.sandbox";

        /// <summary>File the <see cref="LocalJsonProfileStore"/> cache should use.</summary>
        public static string CacheFileName => Sandbox ? "profile.backend-sandbox.json" : "profile.json";

        public static string ProgressUrl => BaseUrl + "/v1/progress";
        public static string PlayerUrl   => BaseUrl + "/v1/player";
        public static string LeaderboardUrl => BaseUrl + "/v1/leaderboard";
    }
}
