using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Tiny glue: an open button shows a panel, a close button hides it. Starts hidden.
    /// Used for the main-menu settings pop-up (AI_Guidelines §1 — one job).
    /// </summary>
    [DisallowMultipleComponent]
    public class PanelToggle : MonoBehaviour
    {
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _panel;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_openButton != null) _openButton.onClick.AddListener(() => Set(true));
            if (_closeButton != null) _closeButton.onClick.AddListener(() => Set(false));
        }

        private void Set(bool on)
        {
            if (_panel != null) _panel.SetActive(on);
        }
    }
}
