using SpaceSurvivors.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Binds volume sliders + a fullscreen toggle to <see cref="SettingsService"/>. Drop it on
    /// a panel and wire the controls; reused by the main menu and the pause screen. Reads the
    /// stored values on enable, writes them back on change — no state of its own (§1).
    /// </summary>
    [DisallowMultipleComponent]
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Toggle _fullscreenToggle;

        [Header("Optional value labels")]
        [SerializeField] private Text _masterValue;
        [SerializeField] private Text _musicValue;
        [SerializeField] private Text _sfxValue;

        private bool _syncing;

        private void Awake()
        {
            if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(v => { if (!_syncing) SettingsService.MasterVolume = v; Refresh(); });
            if (_musicSlider != null)  _musicSlider.onValueChanged.AddListener(v => { if (!_syncing) SettingsService.MusicVolume = v; Refresh(); });
            if (_sfxSlider != null)    _sfxSlider.onValueChanged.AddListener(v => { if (!_syncing) SettingsService.SfxVolume = v; Refresh(); });
            if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.AddListener(v => { if (!_syncing) SettingsService.Fullscreen = v; });
        }

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            _syncing = true;
            if (_masterSlider != null) _masterSlider.value = SettingsService.MasterVolume;
            if (_musicSlider != null)  _musicSlider.value = SettingsService.MusicVolume;
            if (_sfxSlider != null)    _sfxSlider.value = SettingsService.SfxVolume;
            if (_fullscreenToggle != null) _fullscreenToggle.isOn = SettingsService.Fullscreen;
            _syncing = false;

            if (_masterValue != null) _masterValue.text = Percent(SettingsService.MasterVolume);
            if (_musicValue != null)  _musicValue.text = Percent(SettingsService.MusicVolume);
            if (_sfxValue != null)    _sfxValue.text = Percent(SettingsService.SfxVolume);
        }

        private static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
