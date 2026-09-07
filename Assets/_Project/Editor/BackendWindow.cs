using SpaceSurvivors.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// <c>SpaceSurvivors/Backend/Settings</c> — the switchboard for cloud sync.
    ///
    /// <para>Everything here writes <see cref="PlayerPrefs"/>, which is what
    /// <see cref="BackendConfig"/> reads at runtime, so changes take effect on the next Play.
    /// Sync ships <b>off</b>; this window is how it gets turned on.</para>
    /// </summary>
    public sealed class BackendWindow : EditorWindow
    {
        private string _health = "not checked";

        [MenuItem("SpaceSurvivors/Backend/Settings", priority = 0)]
        private static void Open()
        {
            var window = GetWindow<BackendWindow>(utility: false, title: "Backend");
            window.minSize = new Vector2(380, 260);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Cloud sync", EditorStyles.boldLabel);

            bool enabled = EditorGUILayout.Toggle(
                new GUIContent("Enabled", "Install the HTTP stores at startup. Off = purely local play."),
                BackendConfig.Enabled);
            if (enabled != BackendConfig.Enabled) BackendConfig.Enabled = enabled;

            using (new EditorGUI.DisabledScope(!enabled))
            {
                string url = EditorGUILayout.TextField(
                    new GUIContent("Server", "Root URL, no trailing slash."), BackendConfig.BaseUrl);
                if (url != BackendConfig.BaseUrl) BackendConfig.BaseUrl = url;

                string userId = EditorGUILayout.TextField(
                    new GUIContent("Device id", "Sent as X-Device-Id. Change it to play as a second account."),
                    BackendConfig.UserId);
                if (userId != BackendConfig.UserId) BackendConfig.UserId = userId;

                bool sandbox = EditorGUILayout.Toggle(
                    new GUIContent("Sandbox cache",
                        "Use a throwaway local profile file so testing cannot touch the real save."),
                    BackendConfig.Sandbox);
                if (sandbox != BackendConfig.Sandbox) BackendConfig.Sandbox = sandbox;

                EditorGUILayout.HelpBox(
                    BackendConfig.Sandbox
                        ? $"Local cache: {BackendConfig.CacheFileName} — the real profile.json is untouched."
                        : "Local cache: profile.json — this is your REAL save. Tick Sandbox while testing.",
                    BackendConfig.Sandbox ? MessageType.Info : MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("GET /health", _health);

            if (GUILayout.Button("Check server"))
            {
                CheckHealth();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Settings are read at startup, so enter Play mode after changing them. " +
                "The backend must be running: ./mvnw spring-boot:run",
                MessageType.None);
        }

        private void CheckHealth()
        {
            _health = "checking…";
            Repaint();

            var request = UnityWebRequest.Get(BackendConfig.BaseUrl + "/health");
            request.timeout = 5;
            request.SendWebRequest().completed += _ =>
            {
                _health = request.result == UnityWebRequest.Result.Success
                    ? request.downloadHandler.text
                    : $"unreachable ({request.error})";
                request.Dispose();
                Repaint();
            };
        }
    }
}
