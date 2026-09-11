using SpaceSurvivors.Game;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Player-initiated pause. Esc toggles a panel and freezes the game
    /// (<see cref="Time.timeScale"/> = 0). Stays out of the way of the other things that
    /// freeze time — it won't toggle while a level-up screen or the run-end screen owns the
    /// pause. Prefab-style: wires Inspector references, builds nothing (AI_Guidelines §7).
    /// </summary>
    [DisallowMultipleComponent]
    public class PauseScreen : MonoBehaviour
    {
        [SerializeField] private RunController _run;
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _mainGroup;
        [SerializeField] private GameObject _settingsRoot;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _settingsBackButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private string _menuSceneName = "MainMenu";

        private bool _paused;

        private void Awake()
        {
            if (_run == null) _run = FindAnyObjectByType<RunController>();
            if (_root != null) _root.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);

            if (_resumeButton != null) _resumeButton.onClick.AddListener(Resume);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(() => ShowSettings(true));
            if (_settingsBackButton != null) _settingsBackButton.onClick.AddListener(() => ShowSettings(false));
            if (_menuButton != null) _menuButton.onClick.AddListener(ToMenu);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) TogglePause();
        }

        /// <summary>Toggle pause — also callable from a HUD pause button. No-ops when another
        /// screen (level-up, run-end) already owns the freeze.</summary>
        public void TogglePause()
        {
            if (_run != null && _run.RunOver) return;
            if (!_paused && Time.timeScale == 0f) return;   // a level-up owns the pause

            if (_paused) Resume();
            else Pause();
        }

        private void Pause()
        {
            _paused = true;
            Time.timeScale = 0f;
            if (_root != null) _root.SetActive(true);
            ShowSettings(false);
        }

        private void Resume()
        {
            _paused = false;
            if (_root != null) _root.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
            if (_run == null || !_run.RunOver) Time.timeScale = 1f;
        }

        private void ShowSettings(bool on)
        {
            if (_settingsRoot != null) _settingsRoot.SetActive(on);
            if (_mainGroup != null) _mainGroup.SetActive(!on);
        }

        private void ToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_menuSceneName);
        }
    }
}
