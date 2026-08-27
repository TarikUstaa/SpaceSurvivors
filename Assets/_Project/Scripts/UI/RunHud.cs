using SpaceSurvivors.Core;
using SpaceSurvivors.Combat;
using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Minimal in-run HUD: an XP bar with the level number, a survival timer, and a small
    /// health readout. Bootstrap UI built in code (see <see cref="UiBuilder"/>). Reacts to
    /// events; owns no game state.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunHud : MonoBehaviour
    {
        [SerializeField] private LevelSystem _levelSystem;
        [SerializeField] private RunClock _clock;
        [SerializeField] private HealthComponent _playerHealth;
        [SerializeField] private ShieldComponent _shield;

        private Image _xpFill;
        private Text _levelLabel;
        private Text _timerLabel;
        private Text _healthLabel;
        private Text _shieldLabel;

        private void Awake()
        {
            if (_levelSystem == null) _levelSystem = FindFirstObjectByType<LevelSystem>();
            if (_clock == null) _clock = FindFirstObjectByType<RunClock>();
            if (_shield == null) _shield = FindFirstObjectByType<ShieldComponent>();

            UiBuilder.EnsureEventSystem();
            BuildUI();
        }

        private void OnEnable()
        {
            if (_levelSystem != null)
            {
                _levelSystem.XpChanged += HandleXpChanged;
                HandleXpChanged(_levelSystem.XpIntoLevel, _levelSystem.XpForNextLevel);
            }
        }

        private void OnDisable()
        {
            if (_levelSystem != null) _levelSystem.XpChanged -= HandleXpChanged;
        }

        private void HandleXpChanged(int into, int needed)
        {
            // Drive the fill by anchor width — works with no sprite assigned, unlike Image.Filled.
            if (_xpFill != null)
            {
                float f = needed > 0 ? Mathf.Clamp01((float)into / needed) : 0f;
                var rt = _xpFill.rectTransform;
                rt.anchorMax = new Vector2(f, 1f);
            }
            if (_levelLabel != null) _levelLabel.text = $"LV {_levelSystem.CurrentLevel}";
        }

        private void Update()
        {
            if (_timerLabel != null && _clock != null)
            {
                int s = Mathf.FloorToInt(_clock.Elapsed);
                _timerLabel.text = $"{s / 60:00}:{s % 60:00}";
            }
            if (_healthLabel != null && _playerHealth != null)
                _healthLabel.text = $"HP {Mathf.CeilToInt(_playerHealth.Current)}/{Mathf.CeilToInt(_playerHealth.Max)}";

            if (_shieldLabel != null && _shield != null)
            {
                if (_shield.MaxCharges <= 0) _shieldLabel.text = "";
                else _shieldLabel.text = "SHIELD " +
                    new string('●', _shield.CurrentCharges) + new string('○', _shield.MaxCharges - _shield.CurrentCharges);
            }
        }

        private void BuildUI()
        {
            var canvas = UiBuilder.Canvas("HudCanvas", transform, 100);

            // XP bar across the top
            var barBg = UiBuilder.Image("XpBarBG", canvas.transform, new Color(0f, 0f, 0f, 0.55f));
            var bgRt = barBg.rectTransform;
            bgRt.anchorMin = new Vector2(0f, 1f);
            bgRt.anchorMax = new Vector2(1f, 1f);
            bgRt.pivot = new Vector2(0.5f, 1f);
            bgRt.sizeDelta = new Vector2(0f, 26f);
            bgRt.anchoredPosition = Vector2.zero;

            _xpFill = UiBuilder.Image("XpBarFill", barBg.transform, new Color(0.35f, 0.8f, 1f));
            var fillRt = _xpFill.rectTransform;
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f); // width grows via anchorMax.x in HandleXpChanged
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            _levelLabel = UiBuilder.Text("Level", canvas.transform, "LV 1", 28, Color.white);
            _levelLabel.rectTransform.anchorMin = _levelLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            _levelLabel.rectTransform.pivot = new Vector2(0f, 1f);
            _levelLabel.rectTransform.sizeDelta = new Vector2(160, 44);
            _levelLabel.rectTransform.anchoredPosition = new Vector2(16, -34);
            _levelLabel.alignment = TextAnchor.MiddleLeft;

            _timerLabel = UiBuilder.Text("Timer", canvas.transform, "00:00", 34, Color.white);
            _timerLabel.rectTransform.anchorMin = _timerLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _timerLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _timerLabel.rectTransform.sizeDelta = new Vector2(200, 50);
            _timerLabel.rectTransform.anchoredPosition = new Vector2(0, -34);

            _healthLabel = UiBuilder.Text("Health", canvas.transform, "HP", 24, new Color(1f, 0.7f, 0.7f));
            _healthLabel.rectTransform.anchorMin = _healthLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _healthLabel.rectTransform.pivot = new Vector2(1f, 1f);
            _healthLabel.rectTransform.sizeDelta = new Vector2(220, 44);
            _healthLabel.rectTransform.anchoredPosition = new Vector2(-16, -34);
            _healthLabel.alignment = TextAnchor.MiddleRight;

            _shieldLabel = UiBuilder.Text("Shield", canvas.transform, "", 24, new Color(0.55f, 0.85f, 1f));
            _shieldLabel.rectTransform.anchorMin = _shieldLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _shieldLabel.rectTransform.pivot = new Vector2(1f, 1f);
            _shieldLabel.rectTransform.sizeDelta = new Vector2(300, 40);
            _shieldLabel.rectTransform.anchoredPosition = new Vector2(-16, -66);
            _shieldLabel.alignment = TextAnchor.MiddleRight;
        }
    }
}
