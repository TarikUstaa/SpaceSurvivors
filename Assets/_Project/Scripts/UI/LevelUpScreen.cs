using System.Collections.Generic;
using SpaceSurvivors.Data;
using SpaceSurvivors.Game;
using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// The roguelite level-up pause: on <see cref="LevelSystem.LeveledUp"/> it freezes the
    /// game (<see cref="Time.timeScale"/> = 0), shows up to three <see cref="UpgradeData"/>
    /// choices from <see cref="UpgradeService"/>, applies the pick, then resumes — queueing
    /// extra level-ups so a big XP dump still shows one screen at a time.
    ///
    /// Prefab-style (§7): wires an Inspector panel + 3 choice buttons, builds nothing. It
    /// only asks <see cref="UpgradeService"/> to roll/apply and checks <see cref="RunController"/>
    /// so it never pops over a game-over.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelUpScreen : MonoBehaviour
    {
        [SerializeField] private LevelSystem _levelSystem;
        [SerializeField] private UpgradeService _upgrades;
        [SerializeField] private RunController _run;
        [SerializeField] private int _choicesPerLevel = 3;

        [Header("Widgets")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _header;
        [SerializeField] private Button[] _choiceButtons;
        [SerializeField] private Text[] _choiceLabels;
        [SerializeField] private Color _normalTint = new Color(0.16f, 0.22f, 0.34f);
        [SerializeField] private Color _evolutionTint = new Color(0.62f, 0.45f, 0.12f);

        private int _pendingLevelUps;
        private bool _open;

        private void Awake()
        {
            if (_levelSystem == null) _levelSystem = FindAnyObjectByType<LevelSystem>();
            if (_upgrades == null) _upgrades = FindAnyObjectByType<UpgradeService>();
            if (_run == null) _run = FindAnyObjectByType<RunController>();
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (_levelSystem != null) _levelSystem.LeveledUp += HandleLeveledUp;
        }

        private void OnDisable()
        {
            if (_levelSystem != null) _levelSystem.LeveledUp -= HandleLeveledUp;
        }

        private void HandleLeveledUp(int newLevel)
        {
            _pendingLevelUps++;
            if (!_open) ShowNext();
        }

        private void ShowNext()
        {
            if (_run != null && _run.RunOver) { _pendingLevelUps = 0; Close(); return; }
            if (_pendingLevelUps <= 0) { Close(); return; }

            _pendingLevelUps--;
            _open = true;
            Time.timeScale = 0f;
            if (_panel != null) _panel.SetActive(true);

            if (_header != null) _header.text = $"LEVEL {_levelSystem.CurrentLevel}";

            IReadOnlyList<UpgradeOffer> picks = _upgrades.Roll(_choicesPerLevel);
            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                bool has = i < picks.Count;
                _choiceButtons[i].gameObject.SetActive(has);
                if (!has) continue;

                UpgradeOffer pick = picks[i];
                if (i < _choiceLabels.Length && _choiceLabels[i] != null)
                    _choiceLabels[i].text = string.IsNullOrEmpty(pick.Description)
                        ? pick.Title
                        : $"{pick.Title}\n<size=22>{pick.Description}</size>";

                if (_choiceButtons[i].targetGraphic is Image img)
                    img.color = pick.IsEvolution ? _evolutionTint : _normalTint;

                _choiceButtons[i].onClick.RemoveAllListeners();
                var captured = pick;
                _choiceButtons[i].onClick.AddListener(() => Choose(captured));
            }

            if (picks.Count == 0) ShowNext();
        }

        private void Choose(UpgradeOffer pick)
        {
            _upgrades.Apply(pick);
            ShowNext();
        }

        private void Close()
        {
            _open = false;
            if (_panel != null) _panel.SetActive(false);
            if (_run == null || !_run.RunOver) Time.timeScale = 1f;
        }
    }
}
