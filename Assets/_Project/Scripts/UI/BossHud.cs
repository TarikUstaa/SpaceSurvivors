using SpaceSurvivors.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Mini-boss UI: a flashing "warning" banner when <see cref="SpawnDirector.BossIncoming"/>
    /// fires, then a name + health bar along the bottom while a <see cref="BossMarker"/> is
    /// alive. Bootstrap UI (see <see cref="UiBuilder"/>); reacts to events / polls the marker,
    /// owns no game state (AI_Guidelines §1).
    /// </summary>
    [DisallowMultipleComponent]
    public class BossHud : MonoBehaviour
    {
        [SerializeField] private SpawnDirector _spawnDirector;

        private GameObject _warning;
        private Text _warningText;
        private float _warningTimer;

        private GameObject _bar;
        private Text _bossName;
        private RectTransform _fill;

        private void Awake()
        {
            if (_spawnDirector == null) _spawnDirector = FindFirstObjectByType<SpawnDirector>();
            UiBuilder.EnsureEventSystem();
            BuildUI();
            _warning.SetActive(false);
            _bar.SetActive(false);
        }

        private void OnEnable()
        {
            if (_spawnDirector != null) _spawnDirector.BossIncoming += HandleBossIncoming;
        }

        private void OnDisable()
        {
            if (_spawnDirector != null) _spawnDirector.BossIncoming -= HandleBossIncoming;
        }

        private void HandleBossIncoming(string bossName, float lead)
        {
            _warningText.text = $"!!  {bossName.ToUpperInvariant()}  APPROACHING  !!";
            _warningTimer = Mathf.Max(1.5f, lead + 1f);
            _warning.SetActive(true);
        }

        private void Update()
        {
            if (_warningTimer > 0f)
            {
                _warningTimer -= Time.unscaledDeltaTime;
                float a = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
                _warningText.color = new Color(1f, 0.3f, 0.25f, Mathf.Lerp(0.5f, 1f, a));
                if (_warningTimer <= 0f) _warning.SetActive(false);
            }

            var boss = BossMarker.Active;
            bool show = boss != null && boss.Health != null && boss.Health.IsAlive;
            _bar.SetActive(show);
            if (!show) return;

            _bossName.text = boss.DisplayName;
            float f = Mathf.Clamp01(boss.Health.Normalized);
            _fill.anchorMax = new Vector2(f, 1f);
        }

        private void BuildUI()
        {
            var canvas = UiBuilder.Canvas("BossHudCanvas", transform, 300);

            _warning = UiBuilder.Text("Warning", canvas.transform, "!!  MINI-BOSS APPROACHING  !!", 56,
                new Color(1f, 0.3f, 0.25f)).gameObject;
            _warningText = _warning.GetComponent<Text>();
            UiBuilder.Place(_warning.GetComponent<RectTransform>(), new Vector2(0.5f, 0.72f), new Vector2(1400, 120));

            // bottom health bar
            _bar = UiBuilder.Image("BossBarBG", canvas.transform, new Color(0f, 0f, 0f, 0.6f)).gameObject;
            var bgRt = _bar.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.12f, 0f);
            bgRt.anchorMax = new Vector2(0.88f, 0f);
            bgRt.pivot = new Vector2(0.5f, 0f);
            bgRt.sizeDelta = new Vector2(0f, 34f);
            bgRt.anchoredPosition = new Vector2(0f, 24f);

            var fillImg = UiBuilder.Image("Fill", _bar.transform, new Color(0.9f, 0.2f, 0.25f));
            _fill = fillImg.rectTransform;
            _fill.anchorMin = Vector2.zero;
            _fill.anchorMax = new Vector2(1f, 1f);
            _fill.offsetMin = Vector2.zero;
            _fill.offsetMax = Vector2.zero;

            _bossName = UiBuilder.Text("BossName", _bar.transform, "MINI-BOSS", 22, Color.white);
            var nRt = _bossName.rectTransform;
            nRt.anchorMin = new Vector2(0f, 1f);
            nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot = new Vector2(0.5f, 0f);
            nRt.sizeDelta = new Vector2(0f, 28f);
            nRt.anchoredPosition = new Vector2(0f, 4f);
        }
    }
}
