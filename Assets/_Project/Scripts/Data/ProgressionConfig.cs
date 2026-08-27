using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// XP curve for levelling. The cost to go from level L to L+1 is
    /// <c>baseCost + perLevel*(L-1)</c>, then multiplied by <c>softGrowth^(L-1)</c> for a
    /// gentle compounding ramp — early levels come fast, later ones stretch out
    /// (AI_Guidelines §3, no magic numbers in code).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Config/Progression Config", fileName = "ProgressionConfig")]
    public class ProgressionConfig : ScriptableObject
    {
        [Min(1)] public int baseCost = 5;
        [Min(0)] public int perLevel = 4;
        [Tooltip("Compounding factor per level. 1 = purely linear.")]
        [Min(1f)] public float softGrowth = 1.06f;

        [Tooltip("Safety cap so the curve can't explode.")]
        [Min(1)] public int maxCostPerLevel = 4000;

        /// <summary>XP required to advance FROM <paramref name="currentLevel"/> to the next.</summary>
        public int CostForLevel(int currentLevel)
        {
            int l = Mathf.Max(1, currentLevel);
            float linear = baseCost + perLevel * (l - 1);
            float cost = linear * Mathf.Pow(softGrowth, l - 1);
            return Mathf.Clamp(Mathf.RoundToInt(cost), 1, maxCostPerLevel);
        }
    }
}
