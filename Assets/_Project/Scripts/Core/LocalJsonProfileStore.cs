using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Default <see cref="IProfileStore"/>: a JSON file in
    /// <see cref="Application.persistentDataPath"/>. Uses Newtonsoft (not JsonUtility) so the
    /// payload matches what a Jackson backend would produce (Project_Goals §8).
    ///
    /// Reads are defensive — a missing or corrupt file never throws into gameplay, it just
    /// yields a fresh profile (the bad file is copied aside first). Writes go through a temp
    /// file so a crash mid-save can't leave a half-written profile.
    /// </summary>
    public sealed class LocalJsonProfileStore : IProfileStore
    {
        private readonly string _path;

        public LocalJsonProfileStore(string fileName = "profile.json")
            => _path = Path.Combine(Application.persistentDataPath, fileName);

        /// <summary>Absolute path of the profile file (for logs / diagnostics).</summary>
        public string FilePath => _path;

        public PlayerProfile Load()
        {
            try
            {
                if (!File.Exists(_path)) return new PlayerProfile();

                var json = File.ReadAllText(_path);
                var profile = JsonConvert.DeserializeObject<PlayerProfile>(json);
                if (profile == null) return new PlayerProfile();

                Migrate(profile);
                return profile;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Profile] could not read '{_path}' ({e.Message}). " +
                                 "Backing it up and starting a fresh profile.");
                TryBackupCorrupt();
                return new PlayerProfile();
            }
        }

        public void Save(PlayerProfile profile)
        {
            if (profile == null) return;
            try
            {
                profile.schemaVersion = PlayerProfile.CurrentSchemaVersion;
                var json = JsonConvert.SerializeObject(profile, Formatting.Indented);

                var tmp = _path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(_path)) File.Delete(_path);
                File.Move(tmp, _path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Profile] save failed: {e}");
            }
        }

        private static void Migrate(PlayerProfile profile)
        {
            // v1 → v2: metaUpgradeLevels added (M14a). v2 → v3: lifetime-stat counters added
            // (M14c) — additive numeric fields, a missing key just deserialises to 0.
            // Nothing to translate; only normalise the collections so callers never null-check.
            profile.metaUpgradeLevels ??= new System.Collections.Generic.Dictionary<string, int>();
            profile.ownedShipIds ??= new System.Collections.Generic.List<string>();
            profile.unlockedAchievementIds ??= new System.Collections.Generic.List<string>();

            if (profile.schemaVersion < PlayerProfile.CurrentSchemaVersion)
                profile.schemaVersion = PlayerProfile.CurrentSchemaVersion;
        }

        private void TryBackupCorrupt()
        {
            try
            {
                if (File.Exists(_path))
                    File.Copy(_path, $"{_path}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}", overwrite: true);
            }
            catch { /* best effort — never let backup failure escape */ }
        }
    }
}
