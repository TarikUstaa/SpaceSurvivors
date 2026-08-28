using SpaceSurvivors.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Victory / Defeat panel. Reacts to <see cref="RunController.RunEnded"/> — swaps the
    /// header sprite for the outcome, shows the survival time, and offers Replay / Menu.
    /// A real prefab-style component: it wires child references set in the Inspector, it
    /// does not build its own hierarchy (AI_Guidelines §7 — bootstrap-in-code is retired).
    /// </summary>
    [DisallowMultipleComponent]
    public class RunEndScreen : MonoBehaviour
    {
        [SerializeField] private RunController _run;

        [Header("Panel")]
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _headerImage;
        [SerializeField] private Sprite _winHeader;
        [SerializeField] private Sprite _loseHeader;
        [SerializeField] private Text _scoreValue;

        [Header("Buttons")]
        [SerializeField] private Button _replayButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private string _menuSceneName = "MainMenu";

        private void Awake()
        {
            if (_run == null) _run = FindFirstObjectByType<RunController>();
            if (_root != null) _root.SetActive(false);
            if (_replayButton != null) _replayButton.onClick.AddListener(Replay);
            if (_menuButton != null) _menuButton.onClick.AddListener(ToMenu);
        }

        private void OnEnable()
        {
            if (_run != null) _run.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            if (_run != null) _run.RunEnded -= HandleRunEnded;
        }

        private void HandleRunEnded(bool won, float survivedSeconds)
        {
            if (_headerImage != null)
                _headerImage.sprite = won ? _winHeader : _loseHeader;

            if (_scoreValue != null)
            {
                int t = Mathf.FloorToInt(survivedSeconds);
                _scoreValue.text = $"{t / 60:0}:{t % 60:00}";
            }

            if (_root != null) _root.SetActive(true);
        }

        private void Replay()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_menuSceneName);
        }
    }
}
