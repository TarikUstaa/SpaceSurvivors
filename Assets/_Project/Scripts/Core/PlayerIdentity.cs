using System;
using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// The account behind the save — who this player is called, and since when.
    ///
    /// <para>Deliberately separate from <see cref="ProfileService"/>. That owns the save
    /// blob, which the client writes and the server only stores; this owns fields the
    /// <em>server</em> owns and the client may only ask it to change. They travel on
    /// different endpoints for the same reason: a display name should not have to wait for
    /// a run to end to take effect, and a rename can fail in ways a save never does.</para>
    ///
    /// <para>Every call answers through a callback on the main thread and never throws into
    /// the UI. With the backend switched off or unreachable there is no account to speak
    /// of — <see cref="Fetch"/> answers with the cached name and
    /// <see cref="PlayerAccount.FromServer"/> false, and <see cref="Rename"/> reports
    /// <see cref="RenameOutcome.Offline"/>.</para>
    /// </summary>
    public static class PlayerIdentity
    {
        /// <summary>Last name the server confirmed, so a screen can show it before the fetch lands.</summary>
        private const string NameCacheKey = "backend.displayName";

        // Mirrors of the server's rules (PlayerService). Duplicated on purpose: the client
        // can then reject a bad name instantly instead of spending a round trip to be told,
        // and the server still enforces them because a client's checks are never a defence.
        public const int MinNameLength = 3;
        public const int MaxNameLength = 16;

        /// <summary>The name the server last confirmed, or empty if it never has.</summary>
        public static string CachedName => PlayerPrefs.GetString(NameCacheKey, "");

        /// <summary>
        /// True when there is a server to talk to at all. False means the account section
        /// is showing history, not state.
        /// </summary>
        public static bool Available => BackendConfig.Enabled;

        /// <summary>
        /// Read the account. Answers with <see cref="PlayerAccount.FromServer"/> false and
        /// only the cached name when the backend is off or the request does not come back.
        /// </summary>
        public static void Fetch(Action<PlayerAccount> onDone)
        {
            if (onDone == null) return;

            if (!Available)
            {
                onDone(Cached());
                return;
            }

            BackendRequest.Send(BackendConfig.PlayerUrl, "GET", null, (code, body) =>
            {
                if (code != 200)
                {
                    if (code != BackendRequest.NoConnection)
                        Debug.LogWarning($"[Backend] could not read the account ({code}): {body}");
                    onDone(Cached());
                    return;
                }

                var wire = BackendRequest.Parse<PlayerView>(body);
                if (wire == null || string.IsNullOrEmpty(wire.displayName))
                {
                    onDone(Cached());
                    return;
                }

                Remember(wire.displayName);
                onDone(new PlayerAccount
                {
                    PlayerId = wire.playerId ?? "",
                    DisplayName = wire.displayName,
                    Country = wire.country ?? "",
                    FirstLogin = ParseInstant(wire.firstLoginDate),
                    FromServer = true,
                });
            });
        }

        /// <summary>
        /// Ask the server to change the display name.
        ///
        /// <para>The shape of the answer is the point: a rename has three distinct ways to
        /// fail and the player needs to be told which. Taken means try another; invalid
        /// means fix the characters; offline means try later — and only the first two are
        /// the player's to fix.</para>
        /// </summary>
        public static void Rename(string name, Action<RenameOutcome, string> onDone)
        {
            if (onDone == null) return;

            string wanted = (name ?? "").Trim();
            if (!IsValidName(wanted, out string reason))
            {
                // Rejected here rather than after a round trip. The server checks anyway.
                onDone(RenameOutcome.Invalid, reason);
                return;
            }

            if (!Available)
            {
                onDone(RenameOutcome.Offline, "cloud sync is off");
                return;
            }

            string payload = BackendRequest.Serialize(new RenameRequest { displayName = wanted });
            if (payload == null)
            {
                onDone(RenameOutcome.Offline, "could not send the request");
                return;
            }

            BackendRequest.Send(BackendConfig.PlayerUrl, "PATCH", payload, (code, body) =>
            {
                switch (code)
                {
                    case 200:
                        var wire = BackendRequest.Parse<PlayerView>(body);
                        string confirmed = wire != null && !string.IsNullOrEmpty(wire.displayName)
                            ? wire.displayName
                            : wanted;
                        Remember(confirmed);
                        onDone(RenameOutcome.Ok, confirmed);
                        return;

                    case 409:
                        onDone(RenameOutcome.Taken, "that name is taken");
                        return;

                    case 400:
                        // The server saw something our own rules let through. Its wording is
                        // for a developer, so the player gets ours.
                        Debug.LogWarning($"[Backend] rename refused as invalid: {body}");
                        onDone(RenameOutcome.Invalid, NameRule());
                        return;

                    default:
                        if (code != BackendRequest.NoConnection)
                            Debug.LogWarning($"[Backend] rename failed ({code}): {body}");
                        onDone(RenameOutcome.Offline, "could not reach the server");
                        return;
                }
            });
        }

        /// <summary>The server's name rules, checked locally so a bad name costs no round trip.</summary>
        public static bool IsValidName(string name, out string reason)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                reason = "pick a name";
                return false;
            }
            if (name.Length < MinNameLength || name.Length > MaxNameLength)
            {
                reason = $"{MinNameLength}-{MaxNameLength} characters";
                return false;
            }
            foreach (char c in name)
            {
                if (c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_') continue;
                reason = "letters, numbers and _ only";
                return false;
            }
            reason = "";
            return true;
        }

        public static string NameRule() => $"{MinNameLength}-{MaxNameLength} letters, numbers or _";

        private static PlayerAccount Cached() => new()
        {
            PlayerId = "",
            DisplayName = CachedName,
            Country = "",
            FirstLogin = null,
            FromServer = false,
        };

        private static void Remember(string name)
        {
            PlayerPrefs.SetString(NameCacheKey, name);
            PlayerPrefs.Save();
        }

        /// <summary>ISO-8601 from the server to a local <see cref="DateTime"/>, or null if it will not parse.</summary>
        private static DateTime? ParseInstant(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso)) return null;
            return DateTime.TryParse(iso, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed.ToLocalTime()
                : null;
        }

        // ── wire shapes (mirror the backend's api contract) ────────────────────────────

        private sealed class PlayerView
        {
            public string playerId;
            public string displayName;
            public string country;
            public string firstLoginDate;
        }

        private sealed class RenameRequest
        {
            public string displayName;
        }
    }

    /// <summary>What the server knows about this account, as the profile screen needs it.</summary>
    public sealed class PlayerAccount
    {
        public string PlayerId = "";
        public string DisplayName = "";
        public string Country = "";
        public DateTime? FirstLogin;

        /// <summary>False when this came from the local cache because there was no server to ask.</summary>
        public bool FromServer;
    }

    /// <summary>
    /// How a rename ended. Three failures rather than one because the player's next move
    /// differs: choose another name, fix the characters, or wait.
    /// </summary>
    public enum RenameOutcome
    {
        Ok,
        Taken,
        Invalid,
        Offline,
    }
}
