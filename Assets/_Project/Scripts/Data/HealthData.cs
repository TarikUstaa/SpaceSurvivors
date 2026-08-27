using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// Tuning data for a <see cref="SpaceSurvivors.Combat.HealthComponent"/>. One asset per
    /// archetype (PlayerHealth, GruntHealth, MiniBossHealth…). Keeps HP numbers out of code
    /// and lets many instances share one balance source (AI_Guidelines §3).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Combat/Health Data", fileName = "HealthData")]
    public class HealthData : ScriptableObject
    {
        [Tooltip("Starting and maximum hit points.")]
        [Min(1f)] public float maxHealth = 100f;

        [Tooltip("Seconds of invulnerability granted right after taking a hit. 0 = none.")]
        [Min(0f)] public float invulnerabilityAfterHit = 0f;
    }
}
