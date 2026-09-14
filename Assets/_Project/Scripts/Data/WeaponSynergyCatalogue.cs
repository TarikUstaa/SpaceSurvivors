using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>Every weapon synergy in the game. Loaded from Resources at runtime, same
    /// pattern as <see cref="AchievementCatalogue"/>.</summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Combat/Weapon Synergy Catalogue", fileName = "WeaponSynergyCatalogue")]
    public class WeaponSynergyCatalogue : ScriptableObject
    {
        public List<WeaponSynergyData> synergies = new();
    }
}
