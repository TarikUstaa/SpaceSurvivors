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
