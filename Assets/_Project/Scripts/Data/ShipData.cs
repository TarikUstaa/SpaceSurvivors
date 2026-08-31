using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// One buyable ship. A ship is a hull sprite plus a bundle of <see cref="StatModifier"/>s
    /// applied to the player's <see cref="StatSheet"/> at the start of every run — so a ship
    /// is "a permanent build you paid for once" (M14b). Purely data, like
    /// <see cref="MetaUpgradeData"/> (AI_Guidelines §3).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Meta/Ship", fileName = "Ship")]
    public class ShipData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable key stored in the profile. Never rename once shipped.")]
        public string id = "starter";
        public string displayName = "Scout";
        [TextArea] public string description = "";
        [Tooltip("In-game hull sprite (also shown in the hangar).")]
        public Sprite sprite;

        [Header("Economy")]
        [Tooltip("Scrap cost to unlock. 0 = free / owned from the start.")]
        [Min(0)] public int cost = 0;

        [Header("Run-start modifiers")]
        [Tooltip("Applied to the player StatSheet when a run begins.")]
        public StatModifier[] runStartModifiers = System.Array.Empty<StatModifier>();

        public bool IsFree => cost <= 0;
    }
}
