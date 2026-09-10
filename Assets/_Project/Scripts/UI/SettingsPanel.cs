using SpaceSurvivors.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Binds volume sliders + a fullscreen toggle to <see cref="SettingsService"/>. Drop it on
    /// a panel and wire the controls; reused by the main menu and the pause screen. Reads the
    /// stored values on enable, writes them back on change — no state of its own (§1).
    ///
    /// <para>It also carries the cloud-sync switch, which is the only way a player can turn cloud
    /// save on: before this, the setting existed but could be changed from the Unity editor
    /// window alone, so an installed build could never reach the backend at all. Those controls
    /// are optional and left unwired on the in-run pause panel — mid-run is no place to change
    /// where the save lives.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Toggle _fullscreenToggle;

        [Header("Cloud sync (main menu only — leave empty on the pause panel)")]
        [SerializeField] private Toggle _cloudSyncToggle;
        [SerializeField] private Text _cloudSyncStatus;

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
            if (_cloudSyncToggle != null) _cloudSyncToggle.onValueChanged.AddListener(SetCloudSync);
        }

        private void OnEnable()
        {
            Refresh();
            // The status line is the only thing here that changes without the player touching
            // it — a sync can finish, or fail, while the panel is open.
            ProfileService.SyncStatusChanged += OnSyncStatusChanged;
        }

        private void OnDisable() => ProfileService.SyncStatusChanged -= OnSyncStatusChanged;

        private void OnSyncStatusChanged(ProfileSyncStatus _) => RefreshCloudStatus();

        /// <summary>
        /// Turn cloud sync on or off, and make it true immediately rather than at the next
        /// launch. <see cref="BackendBootstrap.Apply"/> is the one place that knows how to put
        /// the stores in the right state, so this asks it rather than repeating the decision.
        /// </summary>
        private void SetCloudSync(bool on)
        {
            if (_syncing) return;
            BackendConfig.Enabled = on;
            BackendBootstrap.Apply();
            RefreshCloudStatus();
        }

        private void RefreshCloudStatus()
        {
            if (_cloudSyncStatus == null) return;

            if (!BackendConfig.Enabled)
            {
                _cloudSyncStatus.text = "progress is kept on this device only";
                return;
            }

            _cloudSyncStatus.text = ProfileService.SyncStatus switch
            {
                ProfileSyncStatus.Synced => "progress is backed up",
                ProfileSyncStatus.Syncing => "syncing…",
                _ => "cannot reach the server — playing from this device",
            };
        }

        private void Refresh()
        {
            _syncing = true;
            if (_masterSlider != null) _masterSlider.value = SettingsService.MasterVolume;
            if (_musicSlider != null)  _musicSlider.value = SettingsService.MusicVolume;
            if (_sfxSlider != null)    _sfxSlider.value = SettingsService.SfxVolume;
            if (_fullscreenToggle != null) _fullscreenToggle.isOn = SettingsService.Fullscreen;
            if (_cloudSyncToggle != null) _cloudSyncToggle.isOn = BackendConfig.Enabled;
            _syncing = false;

            RefreshCloudStatus();

            if (_masterValue != null) _masterValue.text = Percent(SettingsService.MasterVolume);
            if (_musicValue != null)  _musicValue.text = Percent(SettingsService.MusicVolume);
            if (_sfxValue != null)    _sfxValue.text = Percent(SettingsService.SfxVolume);
        }

        private static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
