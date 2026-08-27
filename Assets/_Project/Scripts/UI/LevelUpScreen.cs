using System.Collections.Generic;
using SpaceSurvivors.Core;
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
    /// View only: it asks <see cref="UpgradeService"/> to roll and to apply, and checks
    /// <see cref="RunController"/> so it never pops over a game-over (AI_Guidelines §1).
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelUpScreen : MonoBehaviour
    {
        [SerializeField] private LevelSystem _levelSystem;
        [SerializeField] private UpgradeService _upgrades;
        [SerializeField] private RunController _run;
        [SerializeField] private int _choicesPerLevel = 3;

        private GameObject _panel;
        private Text _header;
        private readonly List<Button> _buttons = new();
        private readonly List<Text> _buttonLabels = new();

        private int _pendingLevelUps;
        private bool _open;

        private void Awake()
        {
            if (_levelSystem == null) _levelSystem = FindFirstObjectByType<LevelSystem>();
            if (_upgrades == null) _upgrades = FindFirstObjectByType<UpgradeService>();
            if (_run == null) _run = FindFirstObjectByType<RunController>();

            UiBuilder.EnsureEventSystem();
            BuildUI();
            _panel.SetActive(false);
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
            _panel.SetActive(true);

            _header.text = $"LEVEL {_levelSystem.CurrentLevel}  —  CHOOSE ONE";

            IReadOnlyList<UpgradeOffer> picks = _upgrades.Roll(_choicesPerLevel);
            for (int i = 0; i < _buttons.Count; i++)
            {
                bool has = i < picks.Count;
                _buttons[i].gameObject.SetActive(has);
                if (!has) continue;

                UpgradeOffer pick = picks[i];
                _buttonLabels[i].text = string.IsNullOrEmpty(pick.Description)
                    ? pick.Title
                    : $"{pick.Title}\n<size=22>{pick.Description}</size>";

                var img = _buttons[i].targetGraphic as UnityEngine.UI.Image;
                if (img != null)
                    img.color = pick.IsEvolution ? new Color(0.55f, 0.42f, 0.1f) : new Color(0.13f, 0.17f, 0.26f);

                _buttons[i].onClick.RemoveAllListeners();
                var captured = pick;
                _buttons[i].onClick.AddListener(() => Choose(captured));
            }

            if (picks.Count == 0) { ShowNext(); } // nothing left to offer — resume / next
        }

        private void Choose(UpgradeOffer pick)
        {
            _upgrades.Apply(pick);
            ShowNext(); // drain any queued level-ups
        }

        private void Close()
        {
            _open = false;
            if (_panel != null) _panel.SetActive(false);
            if (_run == null || !_run.RunOver) Time.timeScale = 1f;
        }

        // ---------------------------------------------------------------- UI

        private void BuildUI()
        {
            var canvas = UiBuilder.Canvas("LevelUpCanvas", transform, 450);

            _panel = UiBuilder.Image("Panel", canvas.transform, new Color(0.04f, 0.05f, 0.09f, 0.92f)).gameObject;
            UiBuilder.Stretch(_panel.GetComponent<RectTransform>());

            _header = UiBuilder.Text("Header", _panel.transform, "LEVEL UP", 44, new Color(0.5f, 0.9f, 1f));
            UiBuilder.Place(_header.rectTransform, new Vector2(0.5f, 0.80f), new Vector2(1400, 120));

            for (int i = 0; i < 3; i++)
            {
                var btn = UiBuilder.Button($"Choice{i}", _panel.transform, new Color(0.13f, 0.17f, 0.26f));
                UiBuilder.Place(btn.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0.62f - i * 0.17f), new Vector2(1000, 150));

                var label = UiBuilder.Text("Label", btn.transform, "", 30, Color.white);
                UiBuilder.Stretch(label.rectTransform, 24f);
                label.supportRichText = true;

                _buttons.Add(btn);
                _buttonLabels.Add(label);
            }
        }
    }
}
