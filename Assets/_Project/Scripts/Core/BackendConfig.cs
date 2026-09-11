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
        private const string DeviceIdKey = "backend.userId";       // key kept: existing installs keep their id
        private const string DeviceSecretKey = "backend.deviceSecret";
        private const string SandboxKey = "backend.sandbox";

        /// <summary>
        /// The deployed service — an Azure Container App in Italy North, in front of a
        /// Postgres Flexible Server. HTTPS is not optional here: the ingress answers a plain
        /// HTTP request with a 301 rather than serving it, and this client sends a device
        /// secret on every token exchange.
        /// <para>A build installed on another machine now reaches this without anyone running
        /// a server. To point at a local one while working on the backend, set
        /// <see cref="BaseUrl"/> — it is stored per install and overrides this.</para>
        /// </summary>
        public const string DefaultBaseUrl =
            "https://spacesurvivors-api.salmonmeadow-a79134b3.italynorth.azurecontainerapps.io";

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
        /// The device's public half of its credential: which device this claims to be.
        ///
        /// <para>Sent only to <see cref="TokenUrl"/>, paired with <see cref="DeviceSecret"/>,
        /// in exchange for a short-lived token; every other request carries that token instead.
        /// (An earlier design sent this on its own as an <c>Authorization: Device …</c> header
        /// and the server believed it — which is why the exchange exists.)</para>
        ///
        /// <para>Generated once and kept, so repeated launches map to the same server-side
        /// player. Safe to read and to log — on its own it proves nothing, because the server
        /// also requires <see cref="DeviceSecret"/>.</para>
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
        /// The device's private half: the proof that this really is that device.
        ///
        /// <para>Generated once, kept on the device, and sent only to <see cref="TokenUrl"/> —
        /// never on an ordinary request. The server stores only a BCrypt hash of it, so a copy
        /// of its database cannot be turned back into this value.</para>
        ///
        /// <para>Two GUIDs, because one is 122 bits of randomness and the server insists on at
        /// least 32 characters. Guessing it is not a realistic attack; reading it off an
        /// unlocked device is, which is the honest limit of device-based identity.</para>
        ///
        /// <para><b>Never log this.</b> It is the account.</para>
        /// </summary>
        public static string DeviceSecret
        {
            get
            {
                var stored = PlayerPrefs.GetString(DeviceSecretKey, "");
                if (!string.IsNullOrWhiteSpace(stored)) return stored;

                var generated = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(DeviceSecretKey, generated);
                PlayerPrefs.Save();
                return generated;
            }
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

        /// <summary>File the <see cref="LocalJsonProfileStore"/> cache should use.</summary>
        public static string CacheFileName => Sandbox ? "profile.backend-sandbox.json" : "profile.json";

        public static string ProgressUrl    => BaseUrl + "/v1/progress";
        public static string PlayerUrl      => BaseUrl + "/v1/player";
        public static string LeaderboardUrl => BaseUrl + "/v1/leaderboard";
        public static string TokenUrl       => BaseUrl + "/v1/auth/token";
    }
}
