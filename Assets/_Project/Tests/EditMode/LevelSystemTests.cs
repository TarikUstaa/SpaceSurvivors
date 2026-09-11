using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpaceSurvivors.Data;
using SpaceSurvivors.Progression;
using UnityEngine;

namespace SpaceSurvivors.Tests
{
    /// <summary>
    /// The XP curve and the levelling bookkeeping that rides on it.
    ///
    /// <para>Levelling is the pacing of a run: how often the upgrade screen interrupts, and how
    /// quickly the player out-scales the spawn ramp. Both are decided by arithmetic that has no
    /// visible failure mode — a wrong rollover does not crash, it just makes the game feel off
    /// in a way a balance pass then tries to fix from the wrong end.</para>
    /// </summary>
    public class LevelSystemTests
    {
        private const int BaseCost = 10;
        private const int PerLevel = 5;

        private ProgressionConfig _config;
        private GameObject _go;
        private LevelSystem _levels;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ProgressionConfig>();
            _config.baseCost = BaseCost;
            _config.perLevel = PerLevel;
            _config.softGrowth = 1f;        // purely linear, so the expected numbers are exact
            _config.maxCostPerLevel = 4000;

            _go = new GameObject(nameof(LevelSystemTests));
            _levels = _go.AddComponent<LevelSystem>();

            // Two pieces of reflection, both because LevelSystem is written for the editor to
            // wire it up rather than for a test to construct it:
            //
            //   * _config is a [SerializeField] private field — in play the Inspector fills it;
            //   * Awake seeds XpForNextLevel, and Unity does not run lifecycle methods on a
            //     component added in edit mode. Without this call XpForNextLevel starts at 0
            //     and the first AddXp grants a free level, which would be a property of the
            //     test harness and not of the game.
            typeof(LevelSystem)
                .GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance)
                !.SetValue(_levels, _config);
            typeof(LevelSystem)
                .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                !.Invoke(_levels, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
        }

        // ── the curve itself ───────────────────────────────────────────────────────────

        [Test]
        public void The_cost_of_a_level_grows_with_the_level()
        {
            Assert.That(_config.CostForLevel(1), Is.EqualTo(BaseCost));
            Assert.That(_config.CostForLevel(2), Is.EqualTo(BaseCost + PerLevel));
            Assert.That(_config.CostForLevel(5), Is.EqualTo(BaseCost + PerLevel * 4));
        }

        [Test]
        public void Soft_growth_compounds_on_top_of_the_linear_part()
        {
            var compounding = ScriptableObject.CreateInstance<ProgressionConfig>();
            compounding.baseCost = 100;
            compounding.perLevel = 0;       // isolate the growth factor
            compounding.softGrowth = 2f;
            compounding.maxCostPerLevel = 100000;

            Assert.That(compounding.CostForLevel(1), Is.EqualTo(100));
            Assert.That(compounding.CostForLevel(2), Is.EqualTo(200));
            Assert.That(compounding.CostForLevel(4), Is.EqualTo(800));

            Object.DestroyImmediate(compounding);
        }

        [Test]
        public void The_cap_stops_the_curve_from_running_away()
        {
            // softGrowth compounds, so without the clamp a long enough run reaches numbers
            // that overflow an int and come back negative — at which point every kill levels
            // the player up forever.
            var steep = ScriptableObject.CreateInstance<ProgressionConfig>();
            steep.baseCost = 10;
            steep.perLevel = 10;
            steep.softGrowth = 2f;
            steep.maxCostPerLevel = 500;

            Assert.That(steep.CostForLevel(50), Is.EqualTo(500));
            Assert.That(steep.CostForLevel(1), Is.GreaterThan(0), "and never below one");

            Object.DestroyImmediate(steep);
        }

        // ── awarding XP ────────────────────────────────────────────────────────────────

        [Test]
        public void A_fresh_level_system_starts_at_level_one_needing_the_first_cost()
        {
            Assert.That(_levels.CurrentLevel, Is.EqualTo(1));
            Assert.That(_levels.XpIntoLevel, Is.Zero);
            Assert.That(_levels.XpForNextLevel, Is.EqualTo(BaseCost));
        }

        [Test]
        public void Xp_short_of_the_cost_does_not_level_up()
        {
            _levels.AddXp(BaseCost - 1);

            Assert.That(_levels.CurrentLevel, Is.EqualTo(1));
            Assert.That(_levels.XpIntoLevel, Is.EqualTo(BaseCost - 1));
        }

        [Test]
        public void Exactly_the_cost_levels_up_and_leaves_nothing_behind()
        {
            _levels.AddXp(BaseCost);

            Assert.That(_levels.CurrentLevel, Is.EqualTo(2));
            Assert.That(_levels.XpIntoLevel, Is.Zero);
            Assert.That(_levels.XpForNextLevel, Is.EqualTo(BaseCost + PerLevel));
        }

        [Test]
        public void Surplus_xp_carries_into_the_next_level_rather_than_being_lost()
        {
            // A single pickup can be worth more than the level it completes. Discarding the
            // remainder would quietly tax the player for collecting XP efficiently.
            _levels.AddXp(BaseCost + 3);

            Assert.That(_levels.CurrentLevel, Is.EqualTo(2));
            Assert.That(_levels.XpIntoLevel, Is.EqualTo(3));
        }

        [Test]
        public void One_large_award_can_cross_several_levels_at_once()
        {
            // Levels 1, 2 and 3 cost 10, 15 and 20 — 45 in total, plus 4 to spare.
            _levels.AddXp(49);

            Assert.That(_levels.CurrentLevel, Is.EqualTo(4));
            Assert.That(_levels.XpIntoLevel, Is.EqualTo(4));
        }

        [Test]
        public void Every_level_crossed_is_announced_separately()
        {
            // The upgrade screen queues one card per level. A single event for a multi-level
            // award would hand the player one card and silently eat the rest.
            var announced = new List<int>();
            _levels.LeveledUp += level => announced.Add(level);

            _levels.AddXp(49);

            Assert.That(announced, Is.EqualTo(new[] { 2, 3, 4 }));
        }

        [Test]
        public void Zero_or_negative_xp_is_ignored()
        {
            _levels.AddXp(0);
            _levels.AddXp(-100);

            Assert.That(_levels.CurrentLevel, Is.EqualTo(1));
            Assert.That(_levels.XpIntoLevel, Is.Zero);
        }

        [Test]
        public void Progress_through_a_level_is_reported_as_a_fraction()
        {
            _levels.AddXp(BaseCost / 2);

            Assert.That(_levels.LevelProgress01, Is.EqualTo(0.5f).Within(0.0001f));
        }

        // ── granted levels (boss rewards) ──────────────────────────────────────────────

        [Test]
        public void A_granted_level_skips_the_cost_and_clears_the_partial_progress()
        {
            // Documented behaviour: the reward is meant to read as a clean "+1 level", so the
            // XP already banked toward it is deliberately discarded rather than carried.
            _levels.AddXp(BaseCost - 1);
            _levels.GrantLevels(1);

            Assert.That(_levels.CurrentLevel, Is.EqualTo(2));
            Assert.That(_levels.XpIntoLevel, Is.Zero);
        }

        [Test]
        public void Granting_several_levels_announces_each_one()
        {
            var announced = new List<int>();
            _levels.LeveledUp += level => announced.Add(level);

            _levels.GrantLevels(3);

            Assert.That(announced, Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(_levels.CurrentLevel, Is.EqualTo(4));
        }

        [Test]
        public void Granting_zero_or_fewer_levels_does_nothing()
        {
            _levels.GrantLevels(0);
            _levels.GrantLevels(-2);

            Assert.That(_levels.CurrentLevel, Is.EqualTo(1));
        }
    }
}
