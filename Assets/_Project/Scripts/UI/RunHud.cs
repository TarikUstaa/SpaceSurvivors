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
        [SerializeField] private Image[] _shieldPips;
        [SerializeField] private Color _shieldPipFull = new Color(0.45f, 0.9f, 1f, 1f);
        [SerializeField] private Color _shieldPipEmpty = new Color(0.22f, 0.3f, 0.42f, 1f);
        [Tooltip("Shows the live scrap total: persistent wallet + what's been collected this run.")]
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
                _scrap.ScrapCollected += HandleScrap;
            ProfileService.Changed += RefreshScrap;
            RefreshScrap();

            if (_shieldGroup != null && _shield != null)
                _shieldGroup.SetActive(_shield.MaxCharges > 0);
        }

        private void OnDisable()
        {
            if (_levelSystem != null) _levelSystem.XpChanged -= HandleXpChanged;
            if (_scrap != null) _scrap.ScrapCollected -= HandleScrap;
            ProfileService.Changed -= RefreshScrap;
        }

        private void HandleXpChanged(int into, int needed)
        {
            if (_xpFill != null) _xpFill.fillAmount = needed > 0 ? Mathf.Clamp01((float)into / needed) : 0f;
            if (_levelLabel != null && _levelSystem != null) _levelLabel.text = $"LV {_levelSystem.CurrentLevel}";
        }

        private void HandleScrap(int _) => RefreshScrap();

        private void RefreshScrap()
        {
            if (_scrapLabel == null) return;
            long total = ProfileService.Wallet + (_scrap != null ? _scrap.TotalScrap : 0);
            _scrapLabel.text = total.ToString("n0");
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
                if (has && _shieldPips != null)
                {
                    for (int i = 0; i < _shieldPips.Length; i++)
                    {
                        if (_shieldPips[i] == null) continue;
                        bool slotExists = i < _shield.MaxCharges;
                        _shieldPips[i].enabled = slotExists;
                        if (slotExists)
                            _shieldPips[i].color = i < _shield.CurrentCharges ? _shieldPipFull : _shieldPipEmpty;
                    }
                }
            }
        }
    }
}
