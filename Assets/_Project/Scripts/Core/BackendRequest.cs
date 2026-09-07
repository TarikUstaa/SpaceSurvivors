using System;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// The little bit of plumbing every backend call needs: fire a JSON request, hand the
    /// status code and body back on the main thread, and never let a network problem become a
    /// gameplay problem.
    ///
    /// <para>Requests ride <see cref="UnityWebRequestAsyncOperation"/>'s completion event rather
    /// than a coroutine, so callers need no MonoBehaviour and nothing blocks a frame.</para>
    /// </summary>
    internal static class BackendRequest
    {
        private const int TimeoutSeconds = 10;

        /// <summary>Status code used for "the request never reached the server".</summary>
        public const long NoConnection = 0;

        /// <summary>
        /// Send an authenticated request, obtaining a token first if there is not a usable one.
        ///
        /// <para>A 401 means the token expired or was rejected, so it is discarded and the
        /// request is retried once with a fresh one. Retrying more than once would loop against
        /// a server that is refusing this device outright.</para>
        /// </summary>
        public static void Send(string url, string method, string body, Action<long, string> onDone)
        {
            BackendSession.WithToken(token =>
            {
                if (token == null)
                {
                    onDone(NoConnection, "");
                    return;
                }

                SendRaw(url, method, body, token, (code, response) =>
                {
                    if (code != 401)
                    {
                        onDone(code, response);
                        return;
                    }

                    BackendSession.Invalidate();
                    BackendSession.WithToken(refreshed =>
                    {
                        if (refreshed == null) onDone(NoConnection, "");
                        else SendRaw(url, method, body, refreshed, onDone);
                    });
                });
            });
        }

        /// <summary>
        /// Fire one request with the token given — or none at all, which is how the token
        /// endpoint itself is called. Everything else should use <see cref="Send"/>.
        /// </summary>
        public static void SendRaw(string url, string method, string body, string token,
                                   Action<long, string> onDone)
        {
            try
            {
                var request = new UnityWebRequest(url, method)
                {
                    downloadHandler = new DownloadHandlerBuffer(),
                    timeout = TimeoutSeconds,
                };

                if (body != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                if (token != null)
                {
                    request.SetRequestHeader("Authorization", "Bearer " + token);
                }

                request.SendWebRequest().completed += _ => Complete(request, onDone);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Backend] could not send {method} {url}: {e.Message}");
                onDone(NoConnection, "");
            }
        }

        private static void Complete(UnityWebRequest request, Action<long, string> onDone)
        {
            long code;
            string body;
            try
            {
                // A 404 or 409 is a ProtocolError but still a real answer from the server, so
                // only a genuine connection failure counts as "no server".
                code = request.result == UnityWebRequest.Result.ConnectionError
                    ? NoConnection
                    : request.responseCode;
                body = request.downloadHandler != null ? request.downloadHandler.text : "";
            }
            catch
            {
                code = NoConnection;
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
                // A bug in our own response handling must not surface as a broken game.
                Debug.LogError($"[Backend] response handling failed: {e}");
            }
        }

        public static string Serialize(object value)
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

        public static T Parse<T>(string json) where T : class
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
    }
}
