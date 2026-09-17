using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Game values an operator can change from the backoffice without a new build — curse chance,
    /// enemy health, spawn rate and so on.
    ///
    /// <para><b>Only overrides travel.</b> The server sends the values somebody has set, nothing
    /// else, and every call here names the game's own value as the fallback. A key the server does
    /// not send — or any key at all while cloud sync is off, or before the first fetch ever succeeds
    /// — is simply the value in the asset, exactly as before this existed.</para>
    ///
    /// <para><b>When a change takes effect.</b> The last good answer is kept in PlayerPrefs and loaded
    /// at startup, and a fresh one is fetched in the background at the same time and on every visit to
    /// the main menu. The systems that read these copy the value when a run starts, so a change never
    /// lands in the middle of a run — it applies from the next one.</para>
    ///
    /// <para>The server re-checks every value against hard limits before sending it; this side still
    /// clamps nothing, because the game's own validation (a chance compared with
    /// <c>Random.value</c>, a multiplier on a curve) already behaves sensibly across those limits.</para>
    /// </summary>
    public static class RemoteConfig
    {
        /// <summary>The keys, spelled once. They must match the backend's <c>GameTunable</c>.</summary>
        public static class Keys
        {
            public const string CurseChance = "curseChance";
            public const string CurseMinLevel = "curseMinLevel";
            public const string EliteChance = "eliteChance";
            public const string EnemyHealthScale = "enemyHealthScale";
            public const string SpawnRateScale = "spawnRateScale";
            public const string XpGainScale = "xpGainScale";
            public const string RelicDropChance = "relicDropChance";
        }

        private const string CacheKey = "remoteConfig.overrides";

        private static Dictionary<string, double> _overrides = new();

        public static float Float(string key, float fallback) =>
            TryGet(key, out var value) ? (float)value : fallback;

        public static int Int(string key, int fallback) =>
            TryGet(key, out var value) ? (int)Math.Round(value) : fallback;

        /// <summary>
        /// Outside Play Mode there are never overrides. EditMode tests build the same components the
        /// game does, and this project runs with domain reload off — so a value fetched in an earlier
        /// play session would otherwise still be sitting in this static and change a test's result
        /// depending on what was last played.
        /// </summary>
        private static bool TryGet(string key, out double value)
        {
            value = 0;
            return Application.isPlaying && BackendConfig.Enabled && _overrides.TryGetValue(key, out value);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            LoadCache();
            Refresh();
        }

        /// <summary>Fetch the current overrides in the background. Does nothing with sync off.</summary>
        public static void Refresh()
        {
            if (!BackendConfig.Enabled) return;

            BackendRequest.SendRaw(BackendConfig.ConfigUrl, "GET", null, null, (code, body) =>
            {
                if (code != 200) return;   // keep the last good answer; offline is not an error
                var response = BackendRequest.Parse<ConfigResponse>(body);
                if (response?.overrides == null) return;

                _overrides = response.overrides;
                PlayerPrefs.SetString(CacheKey, JsonConvert.SerializeObject(_overrides));
                PlayerPrefs.Save();
            }, BackendRequest.WakeTimeoutSeconds);
        }

        private static void LoadCache()
        {
            var cached = PlayerPrefs.GetString(CacheKey, "");
            if (string.IsNullOrEmpty(cached)) return;
            try
            {
                _overrides = JsonConvert.DeserializeObject<Dictionary<string, double>>(cached) ?? new();
            }
            catch (Exception)
            {
                // A corrupt cache is the same as none: the game plays on its own values.
                _overrides = new();
            }
        }

        private sealed class ConfigResponse
        {
            public Dictionary<string, double> overrides;
        }
    }
}
