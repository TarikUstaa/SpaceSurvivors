using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Holds the access token and gets a new one when there isn't a usable one.
    ///
    /// <para>The device secret is exchanged for a short-lived token exactly once per session,
    /// at <see cref="BackendConfig.TokenUrl"/>. Every other request carries the token instead,
    /// so the secret itself is on the wire as rarely as possible — and a token that leaks stops
    /// working on its own, which the old device header never did.</para>
    ///
    /// <para>Requests that arrive while a token is being fetched queue rather than each starting
    /// their own exchange: the game's opening burst is a profile pull and a leaderboard read at
    /// almost the same moment, and three simultaneous logins would be wasted work.</para>
    /// </summary>
    internal static class BackendSession
    {
        /// <summary>Renew a little early, so a request is never sent with a token about to expire.</summary>
        private const double RenewMarginSeconds = 60;

        private static string _token;
        private static double _expiresAt;
        private static bool _fetching;
        private static readonly List<Action<string>> Waiting = new();

        /// <summary>Forget the current token. Called when the server rejects it.</summary>
        public static void Invalidate()
        {
            _token = null;
            _expiresAt = 0;
        }

        /// <summary>
        /// Hand a usable token to <paramref name="onReady"/>, fetching one first if needed.
        /// Passes null when a token cannot be obtained — offline is not an error here, the
        /// caller simply skips its sync.
        /// </summary>
        public static void WithToken(Action<string> onReady)
        {
            if (_token != null && Now < _expiresAt)
            {
                onReady(_token);
                return;
            }

            Waiting.Add(onReady);
            if (_fetching) return;

            _fetching = true;
            RequestToken();
        }

        private static double Now => Time.realtimeSinceStartupAsDouble;

        private static void RequestToken()
        {
            var body = BackendRequest.Serialize(new TokenRequest
            {
                deviceId = BackendConfig.UserId,
                deviceSecret = BackendConfig.DeviceSecret,
            });

            if (body == null)
            {
                Deliver(null);
                return;
            }

            BackendRequest.SendRaw(BackendConfig.TokenUrl, "POST", body, null, OnTokenResponse);
        }

        private static void OnTokenResponse(long code, string body)
        {
            if (code == 200)
            {
                var response = BackendRequest.Parse<TokenResponse>(body);
                if (response != null && !string.IsNullOrEmpty(response.token))
                {
                    _token = response.token;
                    _expiresAt = Now + Math.Max(0, response.expiresIn - RenewMarginSeconds);
                    Deliver(_token);
                    return;
                }
            }
            else if (code == 401)
            {
                // The device id exists but the secret does not match it. Nothing the game can
                // do about that at runtime, and retrying will not help, so say it plainly once.
                Debug.LogError("[Backend] this device's credentials were rejected — cloud sync is off for this session.");
            }
            else if (code != BackendRequest.NoConnection)
            {
                Debug.LogWarning($"[Backend] could not obtain a token ({code}): {body}");
            }

            Deliver(null);
        }

        private static void Deliver(string token)
        {
            _fetching = false;

            // Copy first: a callback may queue another request, and mutating the list while
            // walking it would throw.
            var pending = Waiting.ToArray();
            Waiting.Clear();
            foreach (var callback in pending)
            {
                try
                {
                    callback(token);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Backend] token callback failed: {e}");
                }
            }
        }

        // ── wire shapes (mirror the backend's api contract) ────────────────────────────

        private sealed class TokenRequest
        {
            public string deviceId;
            public string deviceSecret;
        }

        private sealed class TokenResponse
        {
            public string token;
            public long expiresIn;
        }
    }
}
