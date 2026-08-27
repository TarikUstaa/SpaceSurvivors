using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// One level-up choice. Almost every upgrade is just a bag of <see cref="StatModifier"/>s
    /// fed into the player's <see cref="StatSheet"/> — no code per upgrade. A few need a
    /// <see cref="SpecialEffect"/> (granting a whole new weapon); weapon evolutions are
    /// handled separately via <see cref="WeaponData.evolvesInto"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Upgrades/Upgrade Data", fileName = "UpgradeData")]
    public class UpgradeData : ScriptableObject
    {
        public enum SpecialEffect { None, GrantWeapon }

        [Header("Presentation")]
        public string title = "Upgrade";
        [TextArea] public string description = "";
        public Sprite icon;

        [Header("Stat modifiers")]
        public StatModifier[] modifiers = System.Array.Empty<StatModifier>();

        [Header("Special")]
        public SpecialEffect special = SpecialEffect.None;
        [Tooltip("Weapon granted when Special = GrantWeapon.")]
        public WeaponData weaponToGrant;

        [Header("Draft rules")]
        [Tooltip("Relative chance of being offered.")]
        [Min(0f)] public float weight = 1f;
        [Tooltip("Times this may be taken in one run. 0 = unlimited.")]
        [Min(0)] public int maxStacks = 5;
    }
}
