using System;
using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Player-facing options, persisted to <see cref="PlayerPrefs"/>. A static PlayerPrefs
    /// wrapper (like <see cref="Data.GameSession"/>) — not a lifecycle singleton: no instance
    /// to manage, no mutable in-memory graph, trivially testable. Consumers read the volume
    /// properties; the audio players (M10 wave 3) multiply their output by them.
    ///
    /// <see cref="Apply"/> pushes the current values to the engine (master volume, fullscreen)
    /// and is called once on load and again on every change.
    /// </summary>
    public static class SettingsService
    {
        private const string MasterKey = "opt.vol.master";
        private const string MusicKey  = "opt.vol.music";
        private const string SfxKey    = "opt.vol.sfx";
        private const string FullscreenKey = "opt.fullscreen";

        /// <summary>Raised after any setting changes (and once on first load).</summary>
        public static event Action Changed;

        public static float MasterVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, 0.9f));
            set => SetFloat(MasterKey, Mathf.Clamp01(value));
        }

        public static float MusicVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 0.7f));
            set => SetFloat(MusicKey, Mathf.Clamp01(value));
        }

        public static float SfxVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
            set => SetFloat(SfxKey, Mathf.Clamp01(value));
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, 1) != 0;
            set { PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); PlayerPrefs.Save(); Apply(); Changed?.Invoke(); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnBoot() => Apply();

        /// <summary>Push the stored values to the engine.</summary>
        public static void Apply()
        {
            AudioListener.volume = MasterVolume;

            bool fs = Fullscreen;
            if (Screen.fullScreen != fs)
                Screen.fullScreenMode = fs ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        private static void SetFloat(string key, float v)
        {
            PlayerPrefs.SetFloat(key, v);
            PlayerPrefs.Save();
            Apply();
            Changed?.Invoke();
        }
    }
}
