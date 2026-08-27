using SpaceSurvivors.Core;
using SpaceSurvivors.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Game-over view. Listens for <see cref="RunController.RunEnded"/> and shows a panel
    /// with survival time + a restart button.
    ///
    /// Builds its own minimal uGUI hierarchy at runtime so the scene needs no Canvas
    /// wiring — this is bootstrap UI. When the real HUD/menu art lands (M8) this gets
    /// replaced by a proper prefab; nothing else depends on how the panel is constructed
    /// (AI_Guidelines §1).
    /// </summary>
    [DisallowMultipleComponent]
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private RunController _run;

        private GameObject _panel;
        private Text _timeLabel;

        private void Awake()
        {
            if (_run == null) _run = FindFirstObjectByType<RunController>();
            EnsureEventSystem();
            BuildUI();
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (_run != null) _run.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            if (_run != null) _run.RunEnded -= HandleRunEnded;
        }

        private void HandleRunEnded(float survivedSeconds)
        {
            _panel.SetActive(true);
            int total = Mathf.FloorToInt(survivedSeconds);
            _timeLabel.text = $"YOU SURVIVED   {total / 60:00}:{total % 60:00}";
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ---------------------------------------------------------------- UI building

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            // Project uses the Input System package; use its UI module.
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private void BuildUI()
        {
            var canvasGO = new GameObject("GameOverCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvasGO.transform, false);
            Stretch(_panel.GetComponent<RectTransform>());
            _panel.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.06f, 0.86f);

            var title = MakeText("Title", _panel.transform, "GAME OVER", 96);
            title.color = new Color(1f, 0.36f, 0.32f);
            Place(title.rectTransform, new Vector2(0.5f, 0.60f), new Vector2(1200, 160));

            _timeLabel = MakeText("TimeLabel", _panel.transform, "YOU SURVIVED   00:00", 48);
            Place(_timeLabel.rectTransform, new Vector2(0.5f, 0.46f), new Vector2(1200, 120));

            var buttonGO = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(_panel.transform, false);
            Place(buttonGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.30f), new Vector2(360, 92));
            buttonGO.GetComponent<Image>().color = new Color(0.20f, 0.55f, 0.95f);
            buttonGO.GetComponent<Button>().onClick.AddListener(Restart);

            var btnText = MakeText("Text", buttonGO.transform, "RESTART", 36);
            Stretch(btnText.rectTransform);
        }

        private static Text MakeText(string name, Transform parent, string content, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = content;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }
    }
}
