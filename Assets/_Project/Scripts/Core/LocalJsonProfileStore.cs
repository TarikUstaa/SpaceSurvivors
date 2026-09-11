using System;
using System.Collections.Generic;
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
    /// yields a fresh profile (the bad file is copied aside first). Writes go to a temp file
    /// and are moved over the real one in a single step, so a crash mid-save leaves either the
    /// old profile or the new one and never a half-written or absent file.
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

                // Write beside the real file, then swap it in without ever deleting it first.
                //
                // The delete-then-move this replaces had a window — short, but real — in which
                // no profile existed at all. A crash or a kill landing there did not corrupt
                // the save, it removed it, which is the worse of the two outcomes and the one
                // the player notices.
                //
                // File.Replace does the swap in one step. It insists the destination exists, so
                // the very first save takes the plain Move — and there, by definition, there is
                // no profile to lose. (File.Move's overwrite overload would read better, but
                // Unity's .NET profile does not expose it.)
                var tmp = _path + ".tmp";
                File.WriteAllText(tmp, json);

                if (File.Exists(_path))
                {
                    File.Replace(tmp, _path, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(tmp, _path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Profile] save failed: {e}");
            }
        }

        private static void Migrate(PlayerProfile profile)
        {
            // v1 → v2: metaUpgradeLevels added (M14a). v2 → v3: lifetime-stat counters added
            // (M14c). v3 → v4: selectedMapId added (M15). v4 → v5: userId added (backend prep).
            // All additive — numbers default to 0, strings to "". Nothing to translate; only
            // normalise collections so callers never null-check.
            profile.metaUpgradeLevels ??= new Dictionary<string, int>();
            profile.ownedShipIds ??= new List<string>();
            profile.unlockedAchievementIds ??= new List<string>();

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
