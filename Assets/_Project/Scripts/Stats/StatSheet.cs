using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Stats
{
    /// <summary>
    /// The player's live stat modifier stack. Components ask it to transform a base value:
    /// <c>float speed = _stats.Modify(StatId.MoveSpeed, config.moveSpeed);</c>
    ///
    /// Formula per stat: <c>(base + Σflat) · (1 + ΣpercentAdd) · ∏(1 + multiplier)</c>.
    /// This is the whole "modifier pipeline" from the guidelines — one place, data-driven,
    /// no magic numbers, and every consumer stays decoupled from every upgrade (§1, §3).
    /// </summary>
    [DisallowMultipleComponent]
    public class StatSheet : MonoBehaviour
    {
        private sealed class Accumulator
        {
            public float Flat;
            public float PercentAdd;
            public float Mult = 1f;

            public float Apply(float baseValue) => (baseValue + Flat) * (1f + PercentAdd) * Mult;

            public void Add(ModifierOp op, float v)
            {
                switch (op)
                {
                    case ModifierOp.Flat: Flat += v; break;
                    case ModifierOp.PercentAdd: PercentAdd += v; break;
                    case ModifierOp.Multiplier: Mult *= 1f + v; break;
                }
            }
        }

        private readonly Dictionary<StatId, Accumulator> _stats = new();

        /// <summary>Raised whenever modifiers change, so cached consumers (health) can refresh.</summary>
        public event Action Changed;

        public void AddModifier(in StatModifier m)
        {
            Acc(m.stat).Add(m.op, m.value);
            Changed?.Invoke();
        }

        public void AddModifiers(IReadOnlyList<StatModifier> mods)
        {
            if (mods == null || mods.Count == 0) return;
            for (int i = 0; i < mods.Count; i++)
                Acc(mods[i].stat).Add(mods[i].op, mods[i].value);
            Changed?.Invoke();
        }

        /// <summary>Run <paramref name="baseValue"/> through the modifiers registered for <paramref name="id"/>.</summary>
        public float Modify(StatId id, float baseValue)
            => _stats.TryGetValue(id, out var acc) ? acc.Apply(baseValue) : baseValue;

        private Accumulator Acc(StatId id)
        {
            if (!_stats.TryGetValue(id, out var acc))
            {
                acc = new Accumulator();
                _stats[id] = acc;
            }
            return acc;
        }
    }
}
