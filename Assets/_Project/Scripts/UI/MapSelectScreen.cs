using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Map picker (M15). Shown after the player picks a mode on the main menu: a one-at-a-time
    /// carousel over the map catalogue (maps are backdrop themes, all free). PLAY commits the
    /// choice via <see cref="MapService"/> and loads the game; BACK returns to the menu.
    /// Prefab-style — reads the service, pushes text (AI_Guidelines §7). Built by
    /// <c>MapSelectBuilder</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapSelectScreen : MonoBehaviour
    {
        [SerializeField] private Image _preview;
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _descLabel;

        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _playButton;

        [SerializeField] private Button _backButton;
        [SerializeField] private string _menuSceneName = "MainMenu";
        [SerializeField] private string _gameSceneName = "Game";

        private int _index;

        private void Awake()
        {
            EnsureEventSystem();

            if (_prevButton != null) _prevButton.onClick.AddListener(() => Step(-1));
            if (_nextButton != null) _nextButton.onClick.AddListener(() => Step(+1));
            if (_playButton != null) _playButton.onClick.AddListener(Play);
            if (_backButton != null) _backButton.onClick.AddListener(() => SceneManager.LoadScene(_menuSceneName));

            MapService.Changed += Refresh;
        }

        private void OnDestroy() => MapService.Changed -= Refresh;

        private void Start()
        {
            _index = Mathf.Max(0, IndexOf(MapService.SelectedId));
            Refresh();
        }

        private static int IndexOf(string id)
        {
            var maps = MapService.Maps;
            for (int i = 0; i < maps.Count; i++)
                if (maps[i] != null && maps[i].id == id) return i;
            return 0;
        }

        private void Step(int dir)
        {
            int n = MapService.Maps.Count;
            if (n == 0) return;
            _index = ((_index + dir) % n + n) % n;
            Refresh();
        }

        private SpaceSurvivors.Data.MapData Current()
        {
            var maps = MapService.Maps;
            return maps.Count > 0 ? maps[Mathf.Clamp(_index, 0, maps.Count - 1)] : null;
        }

        private void Play()
        {
            var map = Current();
            if (map != null) MapService.Select(map);
            Time.timeScale = 1f;
            SceneManager.LoadScene(_gameSceneName);
        }

        private void Refresh()
        {
            var map = Current();
            if (map == null) return;

            if (_preview != null)
            {
                _preview.sprite = map.backdropSprite != null ? map.backdropSprite : map.previewSprite;
                _preview.color = _preview.sprite != null
                    ? (map.backdropSprite != null
                        ? new Color(map.backdropTint.r, map.backdropTint.g, map.backdropTint.b, 1f)
                        : Color.white)
                    : new Color(
                        Mathf.Lerp(map.cameraBackground.r, map.starfieldTint.r, 0.4f),
                        Mathf.Lerp(map.cameraBackground.g, map.starfieldTint.g, 0.4f),
                        Mathf.Lerp(map.cameraBackground.b, map.starfieldTint.b, 0.4f), 1f);
            }
            if (_nameLabel != null) _nameLabel.text = map.displayName;
            if (_descLabel != null) _descLabel.text = map.description;

            bool many = MapService.Maps.Count > 1;
            if (_prevButton != null) _prevButton.interactable = many;
            if (_nextButton != null) _nextButton.interactable = many;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
