using System.Collections.Generic;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// A stat bonus unlocked by having two specific weapons equipped at the same time — a
    /// reason to pick a combination on purpose instead of whichever upgrade card is biggest.
    /// Pure data, like every other tunable in this project (AI_Guidelines §3): the bonus is
    /// <see cref="StatModifier"/>s, the same struct <see cref="UpgradeData"/> already authors
    /// them with, so it goes through the exact same <see cref="StatSheet"/> pipeline every
    /// other bonus does — no new "synergy damage" code path to keep in sync.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Combat/Weapon Synergy", fileName = "WeaponSynergy")]
    public class WeaponSynergyData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Synergy";
        [TextArea] public string description = "";

        [Header("Requires both")]
        public WeaponData weaponA;
        public WeaponData weaponB;

        [Header("While both are equipped")]
        public List<StatModifier> bonuses = new();
    }
}
