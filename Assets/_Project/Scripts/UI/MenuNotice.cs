using SpaceSurvivors.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// A banner across the top of the main menu for messages from the server: the announcement an
    /// operator published in the backoffice.
    ///
    /// <para>Builds its own Canvas in Awake — the <c>TutorialHints</c> pattern — so it needs no scene
    /// wiring: <see cref="MainMenuScreen"/> adds it at runtime. Hidden until there is something to
    /// say, so a game with sync off, or no announcement, looks exactly as before.</para>
    ///
    /// <para>Placed in the strip above the title, clear of the wallet chip in the top-right corner:
    /// 1360 wide centred on a 1920 reference (ends 26 px short of the wallet), 16 px from the top.
    /// The height follows the text — measured with <c>preferredHeight</c> after it is set, not Best
    /// Fit, which did not engage on the run-end screen — and a full 280-character message is three
    /// lines, which still ends above the title's top line.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class MenuNotice : MonoBehaviour
    {
        private static readonly Color InfoBacking = new(0.05f, 0.12f, 0.2f, 0.88f);
        private static readonly Color WarningBacking = new(0.32f, 0.12f, 0.06f, 0.92f);

        private const float BannerWidth = 1360f;
        private const float MinHeight = 48f;
        private const float PaddingY = 8f;

        private GameObject _banner;
        private Image _backing;
        private Text _label;

        private void Awake() => BuildUi();

        private void Start()
        {
            GameContent.FetchAnnouncement(announcement =>
            {
                // The menu may have been left before a slow reply landed.
                if (this == null) return;
                if (announcement != null) Show(announcement.message, announcement.IsWarning);
            });
        }

        public void Show(string message, bool warning)
        {
            _label.text = message;
            _backing.color = warning ? WarningBacking : InfoBacking;
            _banner.SetActive(true);

            var brt = (RectTransform)_banner.transform;
            brt.sizeDelta = new Vector2(BannerWidth, Mathf.Max(MinHeight, _label.preferredHeight + 2 * PaddingY));
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("MenuNoticeCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _banner = new GameObject("Banner", typeof(RectTransform));
            _banner.transform.SetParent(canvasGo.transform, false);
            _backing = _banner.AddComponent<Image>();
            _backing.raycastTarget = false;
            var brt = (RectTransform)_banner.transform;
            brt.anchorMin = new Vector2(0.5f, 1f);
            brt.anchorMax = new Vector2(0.5f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(BannerWidth, MinHeight);
            brt.anchoredPosition = new Vector2(0f, -16f);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(_banner.transform, false);
            _label = textGo.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                          ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _label.fontSize = 22;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = new Color(0.94f, 0.96f, 1f, 1f);
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.raycastTarget = false;
            var trt = _label.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(24, PaddingY);
            trt.offsetMax = new Vector2(-24, -PaddingY);

            _banner.SetActive(false);
        }
    }
}
