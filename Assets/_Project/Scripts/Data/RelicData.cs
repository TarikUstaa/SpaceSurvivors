using System.Collections.Generic;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// A permanent, run-scoped passive item — found on the battlefield (boss drops, rare
    /// regular-kill drops), not offered on the level-up card screen, so it reads as a
    /// separate kind of power from the 16 upgrade cards. Same shape as <see cref="WeaponSynergyData"/>:
    /// pure data, bonuses are <see cref="StatModifier"/>s through the same <see cref="StatSheet"/>
    /// pipeline every other bonus uses (AI_Guidelines §1, §3).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Progression/Relic", fileName = "Relic")]
    public class RelicData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable key. Never rename once shipped.")]
        public string id = "relic";
        public string displayName = "Relic";
        [TextArea] public string description = "";
        public Sprite icon;

        [Header("Granted once, kept for the rest of the run")]
        public List<StatModifier> bonuses = new();
    }
}
