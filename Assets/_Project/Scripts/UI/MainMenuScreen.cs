using System;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Main menu: pick a <see cref="GameModeData"/>, stash it in <see cref="GameSession"/>,
    /// load the game scene. Wires child references from the Inspector (prefab-style, §7).
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuScreen : MonoBehaviour
    {
        [Serializable]
        public struct ModeButton
        {
            public Button button;
            public GameModeData mode;
        }

        [SerializeField] private ModeButton[] _modes;
        [SerializeField] private string _gameSceneName = "Game";
        [SerializeField] private Button _quitButton;
        [Tooltip("Optional — shows the hovered/last mode's description.")]
        [SerializeField] private Text _descriptionLabel;
        [Tooltip("Optional — shows the persistent scrap wallet.")]
        [SerializeField] private Text _walletLabel;

        private void Awake()
        {
            EnsureEventSystem();
            RefreshWallet();
            ProfileService.Changed += RefreshWallet;

            foreach (var m in _modes)
            {
                if (m.button == null || m.mode == null) continue;
                ModeButton captured = m;
                captured.button.onClick.AddListener(() => Play(captured.mode));

                if (_descriptionLabel != null)
                {
                    var trigger = captured.button.gameObject.AddComponent<EventTrigger>();
                    var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                    entry.callback.AddListener(_ => _descriptionLabel.text = captured.mode.description);
                    trigger.triggers.Add(entry);
                }
            }

            if (_quitButton != null) _quitButton.onClick.AddListener(Application.Quit);
            if (_descriptionLabel != null && _modes.Length > 0 && _modes[0].mode != null)
                _descriptionLabel.text = _modes[0].mode.description;
        }

        private void OnDestroy() => ProfileService.Changed -= RefreshWallet;

        private void RefreshWallet()
        {
            if (_walletLabel != null) _walletLabel.text = $"SCRAP  {ProfileService.Wallet:n0}";
        }

        private void Play(GameModeData mode)
        {
            GameSession.SelectedMode = mode;
            SceneManager.LoadScene(_gameSceneName);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
