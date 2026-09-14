using SpaceSurvivors.Core;
using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Two short hints shown only during the player's very first-ever run — nothing at all
    /// explained the controls or the upgrade screen before this. "First run" is
    /// <c>ProfileService.Current.runsPlayed == 0</c>: that field only increments at run end
    /// (<c>ProfileService.RecordRun</c>), so it reads 0 for the whole duration of the first run
    /// and never again after — no new profile flag needed.
    ///
    /// <para>Builds its own Canvas/Text in Awake, the same idea <c>DamageNumber</c> and
    /// <c>ChainArcView</c> already use for a self-contained visual — drop this on any
    /// always-present GameObject (Systems is fine) and it needs no scene wiring at all. Fade
    /// timing mirrors <see cref="EventBanner"/>'s (hold, then fade) on purpose, so a first-run
    /// hint reads like the same kind of message the game already shows, not a new UI language.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class TutorialHints : MonoBehaviour
    {
        [Tooltip("Optional. Empty = auto-resolve a LevelSystem in the scene.")]
        [SerializeField] private LevelSystem _levelSystem;

        [Tooltip("Shown the moment the run starts.")]
        [TextArea] [SerializeField] private string _startHint =
            "WASD or arrow keys to move. Weapons fire on their own at the nearest enemy.";
        [Tooltip("Shown once, at the first level-up.")]
        [TextArea] [SerializeField] private string _levelUpHint =
            "Pick an upgrade. Building around one or two weapons beats spreading thin.";

        [SerializeField, Min(0.5f)] private float _holdSeconds = 4f;
        [SerializeField, Min(0.05f)] private float _fadeSeconds = 0.6f;

        private Text _label;
        private CanvasGroup _group;
        private bool _firstRun;
        private bool _levelUpHintShown;
        private float _showLeft;

        private void Awake()
        {
            if (_levelSystem == null) _levelSystem = FindAnyObjectByType<LevelSystem>();
            _firstRun = ProfileService.Current.runsPlayed == 0;

            BuildUi();
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("TutorialHintsCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30; // above the HUD, below nothing in particular — a message, not a modal
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _group = canvasGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var backingGo = new GameObject("Backing");
            backingGo.transform.SetParent(canvasGo.transform, false);
            var backing = backingGo.AddComponent<Image>();
            backing.color = new Color(0.02f, 0.03f, 0.06f, 0.72f);
            var brt = backing.rectTransform;
            brt.anchorMin = new Vector2(0.5f, 0f);
            brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.sizeDelta = new Vector2(1000, 90);
            brt.anchoredPosition = new Vector2(0f, 130f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(backingGo.transform, false);
            _label = textGo.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                          ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _label.fontSize = 26;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = new Color(0.9f, 0.95f, 1f, 1f);
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            var trt = _label.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(24f, 8f);
            trt.offsetMax = new Vector2(-24f, -8f);
        }

        private void OnEnable()
        {
            if (_levelSystem != null) _levelSystem.LeveledUp += HandleLeveledUp;
        }

        private void OnDisable()
        {
            if (_levelSystem != null) _levelSystem.LeveledUp -= HandleLeveledUp;
        }

        private void Start()
        {
            if (_firstRun) Show(_startHint);
        }

        private void HandleLeveledUp(int level)
        {
            if (!_firstRun || _levelUpHintShown) return;
            _levelUpHintShown = true;
            Show(_levelUpHint);
        }

        private void Show(string text)
        {
            if (_label == null || string.IsNullOrEmpty(text)) return;
            _label.text = text;
            _showLeft = _holdSeconds + _fadeSeconds;
        }

        private void Update()
        {
            if (_group == null || _showLeft <= 0f) return;
            _showLeft -= Time.deltaTime;
            _group.alpha = _showLeft > _fadeSeconds ? 1f : Mathf.Clamp01(_showLeft / _fadeSeconds);
        }
    }
}
