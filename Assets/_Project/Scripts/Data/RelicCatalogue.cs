using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>Every relic in the game. Loaded from Resources at runtime, same pattern as
    /// <see cref="WeaponSynergyCatalogue"/> / <see cref="AchievementCatalogue"/>.</summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Progression/Relic Catalogue", fileName = "RelicCatalogue")]
    public class RelicCatalogue : ScriptableObject
    {
        public List<RelicData> relics = new();
    }
}
