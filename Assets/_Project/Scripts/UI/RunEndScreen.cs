using System.Collections.Generic;
using System.Linq;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Enemies;
using SpaceSurvivors.Game;
using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Victory / Defeat panel. Reacts to <see cref="RunController.RunEnded"/> — swaps the
    /// header sprite for the outcome, shows the survival time, the run's stats (kills, level,
    /// scrap) and the mode's best time, and offers Replay / Menu.
    /// A real prefab-style component: it wires child references set in the Inspector, it
    /// does not build its own hierarchy (AI_Guidelines §7 — bootstrap-in-code is retired).
    /// </summary>
    [DisallowMultipleComponent]
    public class RunEndScreen : MonoBehaviour
    {
        [SerializeField] private RunController _run;
        [SerializeField] private RunStats _stats;
        [SerializeField] private RelicService _relics;
        [SerializeField] private UpgradeService _upgrades;

        [Header("Panel")]
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _headerImage;
        [SerializeField] private Sprite _winHeader;
        [SerializeField] private Sprite _loseHeader;
        [SerializeField] private Text _scoreValue;
        [SerializeField] private Text _statsValue;
        [SerializeField] private Text _bestValue;

        [Header("Buttons")]
        [SerializeField] private Button _replayButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private string _menuSceneName = "MainMenu";

        private void Awake()
        {
            if (_run == null) _run = FindAnyObjectByType<RunController>();
            if (_stats == null) _stats = FindAnyObjectByType<RunStats>();
            if (_relics == null) _relics = FindAnyObjectByType<RelicService>();
            if (_upgrades == null) _upgrades = FindAnyObjectByType<UpgradeService>();
            if (_root != null) _root.SetActive(false);
            if (_replayButton != null) _replayButton.onClick.AddListener(Replay);
            if (_menuButton != null) _menuButton.onClick.AddListener(ToMenu);
        }

        private void OnEnable()
        {
            if (_run != null) _run.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            if (_run != null) _run.RunEnded -= HandleRunEnded;
        }

        private void HandleRunEnded(bool won, float survivedSeconds)
        {
            if (_headerImage != null)
                _headerImage.sprite = won ? _winHeader : _loseHeader;

            if (_scoreValue != null)
                _scoreValue.text = Clock(survivedSeconds);

            // Bank the run's earnings + history before showing the numbers (Vampire-Survivors
            // rule: you keep the scrap even on a defeat). One seam — ProfileService. Only a share
            // of what was collected is banked, so display what came back, never _stats.Scrap.
            long banked = 0;
            if (_stats != null)
            {
                banked = ProfileService.BankRunScrap(_stats.Scrap);
                ProfileService.RecordRun(_stats.Kills, _stats.Level, survivedSeconds, _stats.BossesDefeated);
            }
            ProfileService.Save();

            // Lifetime stats are now up to date — unlock any achievements they earned (M14c).
            AchievementService.Evaluate();

            if (_statsValue != null && _stats != null)
            {
                string text =
                    $"KILLS  {_stats.Kills:n0}\nLEVEL  {_stats.Level}\n" +
                    // "of" the run total on purpose: the HUD counted every piece picked up, only a
                    // share of it is banked, and a bare "+750" after a run that showed 5,000 reads
                    // as a bug rather than a rule.
                    $"SCRAP  +{banked:n0} of {_stats.Scrap:n0}\nWALLET  {ProfileService.Wallet:n0}";

                if (!won)
                    text += $"\nDEFEATED BY  {DescribeCause(_run.CauseOfDeath)}";

                // One line each, not one per item: RELICS/CURSES compete for the same tight
                // vertical budget as TOP DAMAGE (see the run-end overlap fix), and a run-scoped
                // pickup is worth naming but not worth a whole section.
                if (_relics != null)
                    text += Carrying("RELICS", _relics.Owned.Select(r => r.displayName));
                if (_upgrades != null)
                    text += Carrying("CURSES", _upgrades.TakenCurses.Select(c => c.displayName));

                var top = _stats.TopWeapons(3);
                if (top.Count > 0)
                {
                    text += "\n\nTOP DAMAGE";
                    foreach (var (weapon, damage) in top)
                        text += $"\n{weapon.displayName}  {damage:n0}";
                }

                _statsValue.text = text;
            }

            string modeId = GameSession.SelectedMode != null ? GameSession.SelectedMode.name : "default";
            var runResult = new RunResult(
                survivedSeconds,
                _stats != null ? _stats.Kills : 0,
                _stats != null ? _stats.Level : 1,
                _stats != null ? _stats.BossesDefeated : 0);
            bool record = HighScoreService.Submit(modeId, runResult);
            if (_bestValue != null)
            {
                float best = HighScoreService.BestSeconds(modeId);
                _bestValue.text = record ? $"NEW BEST  {Clock(best)}" : $"BEST  {Clock(best)}";
            }

            if (_root != null) _root.SetActive(true);
        }

        /// <summary>
        /// "" when nothing to show, otherwise one line: a label and up to 2 names — capped so a
        /// bigger catalogue later can't grow this into another overlap bug. The box this feeds
        /// has no room to spare (see <c>bc2c7d2</c>); a section header and one line per item,
        /// the way TOP DAMAGE does it, would cost 4-6 lines instead of 1.
        ///
        /// <para>2, not 3: measured against the actual Text generator (Unity's own
        /// <c>preferredHeight</c>), showing all 3 curses on one line needs a wrap that leaves
        /// the box 0.75px of margin — not a real margin. Capped at 2 the line fits unwrapped
        /// with ~19px to spare, so growth stays predictable instead of depending on where a
        /// browser-style line-wrap happens to break.</para>
        /// </summary>
        private static string Carrying(string label, IEnumerable<string> names)
        {
            var list = names.ToList();
            if (list.Count == 0) return "";

            const int shown = 2;
            string joined = string.Join(", ", list.Take(shown));
            if (list.Count > shown) joined += $" +{list.Count - shown}";
            return $"\n{label}  {joined}";
        }

        private static string Clock(float seconds)
        {
            int t = Mathf.FloorToInt(Mathf.Max(0f, seconds));
            return $"{t / 60:0}:{t % 60:00}";
        }

        /// <summary>An enemy names itself through its EnemyData (readable — "Splitter", not
        /// "Enemy_Splitter(Clone)"); a hazard or space event has no EnemyBrain, so it falls
        /// back to its own GameObject name with the pooling "(Clone)" suffix trimmed off.</summary>
        private static string DescribeCause(DamageInfo? cause)
        {
            if (cause == null || cause.Value.Source == null) return "the swarm";

            var enemy = cause.Value.Source.GetComponent<EnemyBrain>();
            if (enemy != null && enemy.Data != null) return enemy.Data.displayName;

            string raw = cause.Value.Source.name;
            return raw.EndsWith("(Clone)") ? raw[..^7] : raw;
        }

        private void Replay()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_menuSceneName);
        }
    }
}
