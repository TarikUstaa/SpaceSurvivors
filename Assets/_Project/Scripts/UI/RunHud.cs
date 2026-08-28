using SpaceSurvivors.Core;
using SpaceSurvivors.Combat;
using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// In-run HUD — XP bar + level, survival timer, health bar, shield pips, scrap count.
    /// Prefab-style: every widget is an Inspector reference, this component only pushes
    /// values into them from events / per-frame reads (AI_Guidelines §1, §7). Built by
    /// <c>M10UiBuilder</c> with the CraftPix kit.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunHud : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private LevelSystem _levelSystem;
        [SerializeField] private RunClock _clock;
        [SerializeField] private HealthComponent _playerHealth;
        [SerializeField] private ShieldComponent _shield;
        [SerializeField] private ScrapCollector _scrap;

        [Header("Widgets")]
        [SerializeField] private Image _xpFill;
        [SerializeField] private Text _levelLabel;
        [SerializeField] private Text _timerLabel;
        [SerializeField] private Image _healthFill;
        [SerializeField] private Text _healthLabel;
        [SerializeField] private GameObject _shieldGroup;
        [SerializeField] private Text _shieldLabel;
        [SerializeField] private Text _scrapLabel;

        private void Awake()
        {
            if (_levelSystem == null) _levelSystem = FindFirstObjectByType<LevelSystem>();
            if (_clock == null) _clock = FindFirstObjectByType<RunClock>();
            if (_playerHealth == null && _levelSystem != null) _playerHealth = _levelSystem.GetComponent<HealthComponent>();
            if (_shield == null) _shield = FindFirstObjectByType<ShieldComponent>();
            if (_scrap == null) _scrap = FindFirstObjectByType<ScrapCollector>();
        }

        private void OnEnable()
        {
            if (_levelSystem != null)
            {
                _levelSystem.XpChanged += HandleXpChanged;
                HandleXpChanged(_levelSystem.XpIntoLevel, _levelSystem.XpForNextLevel);
            }
            if (_scrap != null)
            {
                _scrap.ScrapCollected += HandleScrap;
                HandleScrap(0);
            }
            if (_shieldGroup != null && _shield != null)
                _shieldGroup.SetActive(_shield.MaxCharges > 0);
        }

        private void OnDisable()
        {
            if (_levelSystem != null) _levelSystem.XpChanged -= HandleXpChanged;
            if (_scrap != null) _scrap.ScrapCollected -= HandleScrap;
        }

        private void HandleXpChanged(int into, int needed)
        {
            if (_xpFill != null) _xpFill.fillAmount = needed > 0 ? Mathf.Clamp01((float)into / needed) : 0f;
            if (_levelLabel != null && _levelSystem != null) _levelLabel.text = _levelSystem.CurrentLevel.ToString();
        }

        private void HandleScrap(int _)
        {
            if (_scrapLabel != null && _scrap != null) _scrapLabel.text = _scrap.TotalScrap.ToString();
        }

        private void Update()
        {
            if (_timerLabel != null && _clock != null)
            {
                int s = Mathf.FloorToInt(_clock.Elapsed);
                _timerLabel.text = $"{s / 60:00}:{s % 60:00}";
            }

            if (_playerHealth != null)
            {
                if (_healthFill != null) _healthFill.fillAmount = _playerHealth.Normalized;
                if (_healthLabel != null)
                    _healthLabel.text = $"{Mathf.CeilToInt(_playerHealth.Current)}/{Mathf.CeilToInt(_playerHealth.Max)}";
            }

            if (_shield != null && _shieldGroup != null)
            {
                bool has = _shield.MaxCharges > 0;
                if (_shieldGroup.activeSelf != has) _shieldGroup.SetActive(has);
                if (has && _shieldLabel != null)
                    _shieldLabel.text = new string('●', _shield.CurrentCharges)
                                      + new string('○', Mathf.Max(0, _shield.MaxCharges - _shield.CurrentCharges));
            }
        }
    }
}
