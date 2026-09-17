using SpaceSurvivors.Data;
using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Small row of pips in the corner of the HUD showing how many relics this run has picked
    /// up so far — same idea as <c>RunHud</c>'s shield pips, just fed by <see cref="RelicService"/>
    /// instead. Builds its own Canvas in Awake (the <c>TutorialHints</c> pattern) so it needs no
    /// scene wiring: drop this on any always-present GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    public class RelicTray : MonoBehaviour
    {
        private const string CatalogueResource = "RelicCatalogue";

        [SerializeField] private RelicService _relicService;
        [SerializeField] private Color _filledColor = new(1f, 0.85f, 0.25f, 1f);
        [SerializeField] private Color _emptyColor = new(1f, 1f, 1f, 0.12f);
        [SerializeField] private Vector2 _slotSize = new(26f, 26f);
        [SerializeField] private float _slotSpacing = 6f;
        [Tooltip("Top-right offset in HUD reference units (1920x1080). Must clear the widgets " +
                 "stacked above it on that edge: ScrapGroup ends at y -158, KillGroup at -216.")]
        [SerializeField] private Vector2 _corner = new(-34f, -228f);

        private Image[] _slots;

        private void Awake()
        {
            if (_relicService == null) _relicService = FindAnyObjectByType<RelicService>();

            var catalogue = Resources.Load<RelicCatalogue>(CatalogueResource);
            BuildUi(catalogue != null ? catalogue.relics.Count : 0);
        }

        private void BuildUi(int slotCount)
        {
            var canvasGo = new GameObject("RelicTrayCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var rowGo = new GameObject("Row", typeof(RectTransform));
            rowGo.transform.SetParent(canvasGo.transform, false);
            var rowRt = (RectTransform)rowGo.transform;
            rowRt.anchorMin = new Vector2(1f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(1f, 1f);
            rowRt.anchoredPosition = _corner;
            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = _slotSpacing;
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            _slots = new Image[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                var slotGo = new GameObject($"Slot{i}");
                slotGo.transform.SetParent(rowGo.transform, false);
                var img = slotGo.AddComponent<Image>();
                img.color = _emptyColor;
                img.rectTransform.sizeDelta = _slotSize;
                _slots[i] = img;
            }
        }

        private void OnEnable()
        {
            if (_relicService == null) return;
            _relicService.RelicGranted += HandleRelicGranted;
            RefreshOwned();
        }

        private void OnDisable()
        {
            if (_relicService != null) _relicService.RelicGranted -= HandleRelicGranted;
        }

        private void HandleRelicGranted(RelicData _) => RefreshOwned();

        private void RefreshOwned()
        {
            if (_slots == null || _relicService == null) return;
            int owned = _relicService.Owned.Count;
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] != null) _slots[i].color = i < owned ? _filledColor : _emptyColor;
        }
    }
}
