using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// One permanent, wallet-bought upgrade line (Damage, Max HP, Speed, Armour…). Each owned
    /// level layers <see cref="perLevel"/> onto the player's <see cref="StatSheet"/> at the
    /// start of every run. Purely data — the shop UI and the run-start applier read it, no
    /// code per upgrade (AI_Guidelines §3).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Meta/Meta Upgrade", fileName = "MetaUpgrade")]
    public class MetaUpgradeData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable key stored in the player profile. Never rename once shipped.")]
        public string id = "damage";
        public string title = "Damage";
        [TextArea] public string description = "";
        public Sprite icon;

        [Header("Effect (applied once per owned level, at run start)")]
        public StatModifier perLevel = new(StatId.Damage, ModifierOp.PercentAdd, 0.08f);
        [Min(1)] public int maxLevel = 10;

        [Header("Cost curve")]
        [Tooltip("Scrap cost of the FIRST level.")]
        [Min(0)] public int baseCost = 50;
        [Tooltip("Each further level costs the previous × this.")]
        [Min(1f)] public float costGrowth = 1.5f;

        /// <summary>Scrap cost to go from <paramref name="currentLevel"/> to the next one.</summary>
        public long CostForNext(int currentLevel) =>
            (long)System.Math.Round(baseCost * System.Math.Pow(costGrowth, currentLevel));
    }
}
