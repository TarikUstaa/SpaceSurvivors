using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// The whole difficulty curve in one asset. Everything is a function of
    /// <b>seconds survived</b> (continuous, not waves) as the GDD requires — designers tune
    /// the shape by dragging curve handles, no code changes (AI_Guidelines §3).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Config/Difficulty Config", fileName = "DifficultyConfig")]
    public class DifficultyConfig : ScriptableObject
    {
        [Header("Curves — X axis is SECONDS survived")]
        [Tooltip("Y = enemies spawned per second.")]
        public AnimationCurve spawnRatePerSecond = AnimationCurve.EaseInOut(0f, 0.6f, 300f, 6f);

        [Tooltip("Y = multiplier applied to each enemy's base health.")]
        public AnimationCurve healthMultiplier = AnimationCurve.Linear(0f, 1f, 300f, 4f);

        [Tooltip("Y = multiplier applied to each enemy's move speed.")]
        public AnimationCurve speedMultiplier = AnimationCurve.Linear(0f, 1f, 300f, 1.4f);

        [Header("Caps")]
        [Tooltip("Hard ceiling on live enemies (performance safety valve).")]
        [Min(1)] public int maxAliveEnemies = 250;

        [Header("Elites — a rare, stronger reroll of a regular timed spawn")]
        [Tooltip("Chance a regular spawn (not a boss, not a split-off child) becomes elite.")]
        [Range(0f, 1f)] public float eliteChance = 0.04f;
        [Tooltip("Multiplies the enemy's already time-scaled health.")]
        [Min(1f)] public float eliteHealthMultiplier = 3f;
        [Tooltip("Multiplies contact damage. Ranged enemies' shot damage is deliberately left " +
                 "alone, same as the time-based difficulty curve leaves it alone.")]
        [Min(1f)] public float eliteDamageMultiplier = 1.6f;
        [Tooltip("Multiplies the scrap (and therefore XP) this kill pays out.")]
        [Min(1f)] public float eliteScrapMultiplier = 4f;
        [Tooltip("Multiplies the sprite's scale — an elite should read as bigger at a glance.")]
        [Min(1f)] public float eliteScale = 1.35f;
        [Tooltip("Sprite tint while elite (and the colour HitFlash lerps back to after a hit).")]
        public Color eliteTint = new(1f, 0.82f, 0.15f);

        [Header("Roster")]
        [Tooltip("Every enemy that can spawn in this run. Eligibility & weight live on each EnemyData.")]
        public List<EnemyData> roster = new();

        [Header("Boss schedule")]
        [Tooltip("Scripted boss arrivals, each fires ONCE when the clock passes triggerTime. " +
                 "Bosses ignore the spawn budget and the alive cap. GDD: first mini-boss at 180s.")]
        public List<BossEntry> bossSchedule = new() { new BossEntry { triggerTime = 180f } };

        public float SpawnRateAt(float seconds) => Mathf.Max(0f, spawnRatePerSecond.Evaluate(seconds));
        public float HealthMultiplierAt(float seconds) => Mathf.Max(0.01f, healthMultiplier.Evaluate(seconds));
        public float SpeedMultiplierAt(float seconds) => Mathf.Max(0.01f, speedMultiplier.Evaluate(seconds));
    }

    [System.Serializable]
    public class BossEntry
    {
        [Min(0f)] public float triggerTime = 180f;
        public EnemyData bossData;
        [Min(1)] public int count = 1;
        [Tooltip("Seconds of on-screen warning before the boss actually spawns.")]
        [Min(0f)] public float warningLead = 3f;
    }
}
