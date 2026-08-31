using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// Every achievement, in display order. One asset in
    /// <c>Resources/AchievementCatalogue</c> so <c>AchievementService</c> and the
    /// achievements screen share it without per-scene wiring (M14c).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Meta/Achievement Catalogue", fileName = "AchievementCatalogue")]
    public class AchievementCatalogue : ScriptableObject
    {
        public List<AchievementData> achievements = new();
    }
}
