using System;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Cloud-save <see cref="IProfileStore"/>, talking to the Spring Boot backend's
    /// <c>/v1/profile</c> endpoints.
    ///
    /// <para><b>Local first, always.</b> Every read is answered from a wrapped
    /// <see cref="LocalJsonProfileStore"/> and every write hits that cache synchronously before
    /// a single byte goes to the network. The game therefore behaves identically offline — the
    /// server is a backup and a second device, never a dependency. Network work happens in the
    /// background and reports through <see cref="SyncStatus"/>; it never throws into gameplay
    /// and never blocks a frame.</para>
    ///
    /// <para><b>Convergence.</b> Startup does GET then PUT: pull whatever the server has, merge
    /// it into the live profile (<see cref="ProfileMerge"/>), push the result back so both sides
    /// agree. Afterwards each <see cref="Save"/> pushes with the version we last saw; a 409
    /// means someone else wrote first, so we merge their copy in and retry.</para>
    ///
    /// <para>The profile instance handed out by <see cref="Load"/> is mutated <b>in place</b>
    /// when a sync brings new data — callers (notably <see cref="ProfileService"/>) hold that
    /// reference for the whole session, so swapping in a new object would strand them on a
    /// stale copy. <see cref="ProfileRefreshed"/> tells them to redraw.</para>
    /// </summary>
    public sealed class HttpProfileStore : IRemoteProfileStore
    {
        private const int TimeoutSeconds = 10;

        /// <summary>A 409 should resolve on the retry; more than this means something is wrong.</summary>
        private const int MaxConflictRetries = 2;

        private readonly IProfileStore _cache;
        private readonly string _url;
        private readonly string _userId;

        private PlayerProfile _live;
        private ProfileSyncStatus _status = ProfileSyncStatus.Syncing;

        /// <summary>Version the server last reported. 0 = we have never had a successful exchange.</summary>
        private int _serverVersion;

        /// <summary>The opening GET has finished (either way). Pushes wait for it.</summary>
        private bool _pulled;

        private bool _pushInFlight;
        private bool _pushQueued;
        private int _conflictRetries;

        public HttpProfileStore(string url, string userId, IProfileStore cache = null)
        {
            _url = url;
            _userId = userId;
            _cache = cache ?? new LocalJsonProfileStore();
        }

        public ProfileSyncStatus SyncStatus => _status;
        public event Action<ProfileSyncStatus> SyncStatusChanged;
        public event Action ProfileRefreshed;

        // ── IProfileStore ──────────────────────────────────────────────────────────────

        public PlayerProfile Load()
        {
            _live = _cache.Load();
            if (string.IsNullOrEmpty(_live.userId)) _live.userId = _userId;

            Pull();
            return _live;   // immediately — the pull lands later and merges into this instance
        }

        public void Save(PlayerProfile profile)
        {
            if (profile == null) return;

            _live = profile;
            _cache.Save(profile);   // the network may never come back; the disk always works
            Push();
        }

        // ── IRemoteProfileStore ────────────────────────────────────────────────────────

        public void ForceSync()
        {
            if (_live == null) return;
            _conflictRetries = 0;
            Pull();
        }

        // ── pull ───────────────────────────────────────────────────────────────────────

        private void Pull()
        {
            SetStatus(ProfileSyncStatus.Syncing);
            Send("GET", null, OnPullDone);
        }

        private void OnPullDone(long code, string body)
        {
            _pulled = true;

            switch (code)
            {
                case 404:
                    // Nothing stored for this player yet: our save is the only truth. Upload it.
                    _serverVersion = 0;
                    Push();
                    return;

                case 200:
                    var response = Parse<LoadResponse>(body);
                    if (response?.profile == null)
                    {
                        SetStatus(ProfileSyncStatus.Error);
                        return;
                    }

                    _serverVersion = response.version;
                    if (ProfileMerge.MergeInto(_live, response.profile))
                    {
                        _cache.Save(_live);
                        ProfileRefreshed?.Invoke();
                    }

                    // Push unconditionally: the merge may have left us ahead of the server even
                    // when nothing local changed, and one extra write is cheaper than tracking
                    // which side won field by field.
                    Push();
                    return;

                default:
                    // Offline is not an error the player should see — the game is fine without us.
                    SetStatus(code == 0 ? ProfileSyncStatus.Offline : ProfileSyncStatus.Error);
                    if (code != 0) Debug.LogWarning($"[Backend] profile pull failed ({code}): {body}");
                    return;
            }
        }

        // ── push ───────────────────────────────────────────────────────────────────────

        private void Push()
        {
            if (_live == null) return;

            // Pushing before the opening pull would send version 0 and guarantee a conflict.
            if (!_pulled || _pushInFlight)
            {
                _pushQueued = true;
                return;
            }

            var body = Serialize(new SaveRequest { profile = _live, version = _serverVersion });
            if (body == null)
            {
                SetStatus(ProfileSyncStatus.Error);
                return;
            }

            _pushInFlight = true;
            _pushQueued = false;
            SetStatus(ProfileSyncStatus.Syncing);
            Send("PUT", body, OnPushDone);
        }

        private void OnPushDone(long code, string body)
        {
            _pushInFlight = false;

            switch (code)
            {
                case 200:
                    var saved = Parse<SaveResponse>(body);
                    if (saved != null) _serverVersion = saved.version;
                    _conflictRetries = 0;
                    SetStatus(ProfileSyncStatus.Synced);
                    if (_pushQueued) Push();   // a save landed while this one was in flight
                    return;

                case 409:
                    // Someone else wrote first. The server handed back its copy; fold it in and
                    // try again with the version it told us about.
                    var conflict = Parse<ConflictResponse>(body);
                    if (conflict?.profile == null)
                    {
                        SetStatus(ProfileSyncStatus.Error);
                        return;
                    }

                    _serverVersion = conflict.serverVersion;
                    if (ProfileMerge.MergeInto(_live, conflict.profile))
                    {
                        _cache.Save(_live);
                        ProfileRefreshed?.Invoke();
                    }

                    if (_conflictRetries++ < MaxConflictRetries)
                    {
                        Push();
                    }
                    else
                    {
                        _conflictRetries = 0;
                        SetStatus(ProfileSyncStatus.Error);
                        Debug.LogWarning("[Backend] profile kept conflicting; giving up until the next save.");
                    }
                    return;

                default:
                    SetStatus(code == 0 ? ProfileSyncStatus.Offline : ProfileSyncStatus.Error);
                    if (code != 0) Debug.LogWarning($"[Backend] profile push failed ({code}): {body}");
                    return;
            }
        }

        // ── plumbing ───────────────────────────────────────────────────────────────────

        private void SetStatus(ProfileSyncStatus status)
        {
            if (_status == status) return;
            _status = status;
            SyncStatusChanged?.Invoke(status);
        }

        /// <summary>
        /// Fire a request and hand the result to <paramref name="onDone"/> on the main thread.
        /// Uses the async operation's completion event rather than a coroutine so the store
        /// needs no MonoBehaviour of its own. A code of 0 means "never reached the server".
        /// </summary>
        private void Send(string method, string body, Action<long, string> onDone)
        {
            try
            {
                var request = new UnityWebRequest(_url, method)
                {
                    downloadHandler = new DownloadHandlerBuffer(),
                    timeout = TimeoutSeconds,
                };

                if (body != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                // Stand-in for real auth — see BackendConfig.UserId.
                request.SetRequestHeader("X-Dev-User", _userId);

                request.SendWebRequest().completed += _ => Complete(request, onDone);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Backend] could not send {method}: {e.Message}");
                onDone(0, "");
            }
        }

        private static void Complete(UnityWebRequest request, Action<long, string> onDone)
        {
            long code;
            string body;
            try
            {
                // A 404 or 409 is a ProtocolError but still a real answer, so only a genuine
                // connection failure counts as "no server".
                code = request.result == UnityWebRequest.Result.ConnectionError ? 0 : request.responseCode;
                body = request.downloadHandler != null ? request.downloadHandler.text : "";
            }
            catch
            {
                code = 0;
                body = "";
            }
            finally
            {
                request.Dispose();
            }

            try
            {
                onDone(code, body);
            }
            catch (Exception e)
            {
                // A bug in our own handling must not surface as a broken game.
                Debug.LogError($"[Backend] response handling failed: {e}");
            }
        }

        private static string Serialize(object value)
        {
            try
            {
                return JsonConvert.SerializeObject(value);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Backend] could not serialise request: {e.Message}");
                return null;
            }
        }

        private static T Parse<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Backend] could not read response: {e.Message}");
                return null;
            }
        }

        // ── wire shapes (mirror the backend's api contract) ────────────────────────────

        private sealed class SaveRequest
        {
            public PlayerProfile profile;
            public int version;
        }

        private sealed class SaveResponse
        {
            public int version;
        }

        private sealed class LoadResponse
        {
            public PlayerProfile profile;
            public int version;
        }

        private sealed class ConflictResponse
        {
            public int serverVersion;
            public PlayerProfile profile;
        }
    }
}
