using System;

namespace SpaceSurvivors.Stats
{
    /// <summary>
    /// One stat change contributed by an upgrade (or later: an item, a buff, a curse).
    /// Serializable so it authors directly inside an <see cref="SpaceSurvivors.Data.UpgradeData"/>
    /// asset — no code per upgrade (AI_Guidelines §3).
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        public StatId stat;
        public ModifierOp op;
        public float value;

        public StatModifier(StatId stat, ModifierOp op, float value)
        {
            this.stat = stat;
            this.op = op;
            this.value = value;
        }
    }
}
