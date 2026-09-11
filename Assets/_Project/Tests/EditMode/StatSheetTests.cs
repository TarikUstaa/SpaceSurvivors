using NUnit.Framework;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Tests
{
    /// <summary>
    /// The modifier pipeline: <c>(base + Σflat) · (1 + ΣpercentAdd) · ∏(1 + multiplier)</c>.
    ///
    /// <para>Every upgrade in the game is a <see cref="StatModifier"/> fed through here, so this
    /// formula is the difference between a balance pass meaning what it says and not. The order
    /// of operations in particular is invisible until two upgrades of different kinds stack —
    /// which is exactly when a player notices and nobody can reproduce it.</para>
    /// </summary>
    public class StatSheetTests
    {
        private GameObject _go;
        private StatSheet _stats;

        [SetUp]
        public void SetUp()
        {
            // StatSheet has no serialized dependencies and does no work in Awake, so a bare
            // component is the real thing rather than a stand-in.
            _go = new GameObject(nameof(StatSheetTests));
            _stats = _go.AddComponent<StatSheet>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void An_untouched_stat_returns_its_base_value()
        {
            Assert.That(_stats.Modify(StatId.MoveSpeed, 7.5f), Is.EqualTo(7.5f));
        }

        [Test]
        public void Flat_modifiers_add_before_the_percentage_applies()
        {
            // (10 + 5) * (1 + 0.5) = 22.5, not 10 * 1.5 + 5 = 20. The difference is the whole
            // reason the formula is written down: "+5 damage" is worth more on a build that
            // also has "+50% damage", and that is the intended interaction.
            _stats.AddModifier(new StatModifier(StatId.Damage, ModifierOp.Flat, 5f));
            _stats.AddModifier(new StatModifier(StatId.Damage, ModifierOp.PercentAdd, 0.5f));

            Assert.That(_stats.Modify(StatId.Damage, 10f), Is.EqualTo(22.5f).Within(0.0001f));
        }

        [Test]
        public void Percent_add_modifiers_sum_while_multipliers_compound()
        {
            // Two +50% PercentAdd give +100%, not +125%. Two ×1.5 multipliers give ×2.25.
            // Same numbers, different stacking, and the distinction is what stops a handful of
            // upgrades from turning into a runaway.
            _stats.AddModifier(new StatModifier(StatId.Damage, ModifierOp.PercentAdd, 0.5f));
            _stats.AddModifier(new StatModifier(StatId.Damage, ModifierOp.PercentAdd, 0.5f));
            Assert.That(_stats.Modify(StatId.Damage, 10f), Is.EqualTo(20f).Within(0.0001f));

            _stats.AddModifier(new StatModifier(StatId.FireRate, ModifierOp.Multiplier, 0.5f));
            _stats.AddModifier(new StatModifier(StatId.FireRate, ModifierOp.Multiplier, 0.5f));
            Assert.That(_stats.Modify(StatId.FireRate, 10f), Is.EqualTo(22.5f).Within(0.0001f));
        }

        [Test]
        public void Removing_a_modifier_restores_the_value_it_had_before()
        {
            // Timed power-ups add on pickup and remove on expiry. If removal does not land
            // exactly back on the old value, every pickup leaves a residue and a long run
            // drifts — slowly enough that it reads as a balance problem rather than a bug.
            const float baseValue = 12f;
            float before = _stats.Modify(StatId.MoveSpeed, baseValue);

            var buff = new StatModifier(StatId.MoveSpeed, ModifierOp.Multiplier, 0.4f);
            _stats.AddModifier(buff);
            Assert.That(_stats.Modify(StatId.MoveSpeed, baseValue), Is.Not.EqualTo(before));

            _stats.RemoveModifier(buff);
            Assert.That(_stats.Modify(StatId.MoveSpeed, baseValue), Is.EqualTo(before).Within(0.0001f));
        }

        [Test]
        public void Removing_a_minus_one_hundred_percent_multiplier_does_not_divide_by_zero()
        {
            // ×(1 + -1) is ×0: the stat is pinned at zero and there is no factor to undo. The
            // guard leaves it alone rather than producing infinity or NaN, which would spread
            // to every consumer of the stat and be very hard to trace back here.
            var kill = new StatModifier(StatId.Damage, ModifierOp.Multiplier, -1f);
            _stats.AddModifier(kill);
            _stats.RemoveModifier(kill);

            float result = _stats.Modify(StatId.Damage, 10f);
            Assert.That(float.IsNaN(result), Is.False);
            Assert.That(float.IsInfinity(result), Is.False);
        }

        [Test]
        public void Modifiers_on_one_stat_leave_every_other_stat_alone()
        {
            _stats.AddModifier(new StatModifier(StatId.Damage, ModifierOp.PercentAdd, 2f));

            Assert.That(_stats.Modify(StatId.MoveSpeed, 6f), Is.EqualTo(6f));
            Assert.That(_stats.Modify(StatId.PickupRadius, 1f), Is.EqualTo(1f));
        }

        [Test]
        public void Adding_a_batch_raises_Changed_once_rather_than_per_modifier()
        {
            // An upgrade card can carry several modifiers. HealthComponent recalculates its
            // maximum on this event, so firing per modifier would recompute against a
            // half-applied stack — briefly wrong, and whichever value lands last sticks.
            int raised = 0;
            _stats.Changed += () => raised++;

            _stats.AddModifiers(new[]
            {
                new StatModifier(StatId.Damage, ModifierOp.Flat, 1f),
                new StatModifier(StatId.Damage, ModifierOp.PercentAdd, 0.1f),
                new StatModifier(StatId.MoveSpeed, ModifierOp.Flat, 0.5f),
            });

            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void An_empty_or_null_batch_changes_nothing_and_raises_nothing()
        {
            int raised = 0;
            _stats.Changed += () => raised++;

            _stats.AddModifiers(null);
            _stats.AddModifiers(new StatModifier[0]);

            Assert.That(raised, Is.Zero);
        }
    }
}
