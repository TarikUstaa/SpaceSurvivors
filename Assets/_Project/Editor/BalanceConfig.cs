using System.Collections.Generic;
using System.Linq;
using SpaceSurvivors.Data;
using UnityEditor;
using UnityEngine;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// M16 balance pass — the single source of truth for the difficulty curves, enemy roster
    /// timing, boss schedules and XP curve. Editing AnimationCurve keyframes in YAML is
    /// error-prone, so all of it is applied programmatically here (`SpaceSurvivors/Balance/
    /// M16 Apply balance`). Re-run after every tweak; the numbers below are the knobs.
    ///
    /// Targets (GDD §5): 0:00–3:00 gentle ramp, no elites; mini-boss at exactly 180s; player
    /// power ≈ doubles / 2 levels; ~1 level / 20–30s early; Campaign win ≈ 15 min.
    /// </summary>
    internal static class BalanceConfig
    {
        private const string Cfg = "Assets/_Project/ScriptableObjects/Config/";
        private const string Enm = "Assets/_Project/ScriptableObjects/Enemies/";

        [MenuItem("SpaceSurvivors/Balance/M16 Apply balance")]
        private static void Apply()
        {
            var infinite = Load<DifficultyConfig>(Cfg + "DifficultyConfig.asset");
            var campaign = Load<DifficultyConfig>(Cfg + "CampaignDifficulty.asset");
            var mini = Load<EnemyData>(Enm + "MiniBoss.asset");
            var final = Load<EnemyData>(Enm + "FinalBoss.asset");

            // ---- roster timing (shared) — first 3:00 is Grunt / Swarmer / Shooter only ----
            SetEnemy("Grunt",    earliest: 0f,   weight: 1.0f);
            SetEnemy("Swarmer",  earliest: 20f,  weight: 1.3f);
            SetEnemy("Shooter",  earliest: 50f,  weight: 0.55f);
            SetEnemy("Charger",  earliest: 185f, weight: 0.55f);
            SetEnemy("Splitter", earliest: 210f, weight: 0.5f);
            SetEnemy("Brute",    earliest: 300f, weight: 0.4f);

            // ---- Infinite: endless escalation. Iter-1 first 3:00 was fine (HP held 60-80, never
            //      threatened) but the mid/late game was trivial for a focused build; iter-2's
            //      harder EARLY ramp killed the bot at 0:90. So: keep iter-1's first 3:00 exactly,
            //      then ramp HP/spawn hard so the snowball loses the race by ~15-20 min. ----
            infinite.spawnRatePerSecond = Curve(
                (0, 0.35f), (30, 0.65f), (60, 1.05f), (120, 1.7f), (180, 2.4f),
                (300, 4f), (480, 6f), (700, 9f), (1000, 13f));
            infinite.healthMultiplier = Curve(
                (0, 1f), (60, 1.12f), (150, 1.45f), (180, 1.6f),
                (300, 2.6f), (480, 4.2f), (700, 7f), (1000, 11f));
            infinite.speedMultiplier = Curve(
                (0, 1f), (120, 1.05f), (180, 1.09f), (400, 1.17f), (800, 1.29f), (1100, 1.4f));
            infinite.maxAliveEnemies = 280;
            infinite.bossSchedule = new List<BossEntry>
            {
                Boss(180f, mini, 1, 4f),
                Boss(420f, mini, 2, 4f),
                Boss(660f, mini, 3, 4f),
                Boss(900f, final, 1, 5f),
                Boss(1200f, final, 2, 5f),
            };

            // ---- Campaign: paced to a ~15-min, 5-stage win. Iter-1 early ramp (kept), then a
            //      firm — but slightly gentler than Infinite — mid/late so the final-boss stage
            //      is a climax, not a wall. ----
            campaign.spawnRatePerSecond = Curve(
                (0, 0.35f), (30, 0.65f), (60, 1.05f), (120, 1.7f), (180, 2.35f),
                (300, 3.6f), (480, 5.2f), (660, 6.6f), (900, 8f));
            campaign.healthMultiplier = Curve(
                (0, 1f), (60, 1.12f), (150, 1.45f), (180, 1.6f),
                (300, 2.4f), (480, 3.7f), (720, 5.3f), (900, 6.5f));
            campaign.speedMultiplier = Curve(
                (0, 1f), (120, 1.05f), (180, 1.09f), (400, 1.15f), (700, 1.24f), (900, 1.3f));
            campaign.maxAliveEnemies = 240;
            campaign.bossSchedule = new List<BossEntry>
            {
                Boss(180f, mini, 1, 4f),   // stage 1 -> 2
                Boss(420f, mini, 2, 4f),   // stage 2 -> 3
                Boss(660f, mini, 2, 5f),   // stage 3 -> 4
                Boss(900f, final, 1, 6f),  // stage 4 -> 5 (win)
            };

            // ---- XP curve — iter-2's steeper curve starved the early build and killed the bot
            //      at 0:90; back near iter-1 (early cadence ~1 level/18-22s is fine; keeping the
            //      player one level behind spirals). ----
            var prog = Load<ProgressionConfig>(Cfg + "ProgressionConfig.asset");
            prog.baseCost = 5;
            prog.perLevel = 5;
            prog.softGrowth = 1.06f;
            prog.maxCostPerLevel = 4000;

            // ---- upgrade ceilings — trim the raw-DPS snowball so weapons/utility matter ----
            SetUpgradeMaxStacks("Damage", 4);
            SetUpgradeMaxStacks("FireRate", 6);

            foreach (var o in new Object[] { infinite, campaign, prog }) EditorUtility.SetDirty(o);
            AssetDatabase.SaveAssets();
            Debug.Log("[BalanceConfig] applied M16 balance (curves + roster + bosses + XP).");
        }

        // ---------------------------------------------------------------- helpers

        private static T Load<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) Debug.LogError($"[BalanceConfig] missing asset: {path}");
            return a;
        }

        private static AnimationCurve Curve(params (float t, float v)[] keys)
        {
            var c = new AnimationCurve(keys.Select(k => new Keyframe(k.t, k.v)).ToArray());
            for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
            c.preWrapMode = WrapMode.ClampForever;
            c.postWrapMode = WrapMode.ClampForever;
            return c;
        }

        private static BossEntry Boss(float t, EnemyData data, int count, float lead) =>
            new() { triggerTime = t, bossData = data, count = count, warningLead = lead };

        private static void SetEnemy(string name, float earliest, float weight)
        {
            var e = Load<EnemyData>(Enm + name + ".asset");
            if (e == null) return;
            var so = new SerializedObject(e);
            so.FindProperty("earliestSpawnTime").floatValue = earliest;
            so.FindProperty("spawnWeight").floatValue = weight;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(e);
        }

        private static void SetUpgradeMaxStacks(string name, int maxStacks)
        {
            var u = AssetDatabase.LoadAssetAtPath<UpgradeData>(
                "Assets/_Project/ScriptableObjects/Upgrades/" + name + ".asset");
            if (u == null) { Debug.LogWarning($"[BalanceConfig] no upgrade '{name}'"); return; }
            var so = new SerializedObject(u);
            so.FindProperty("maxStacks").intValue = maxStacks;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(u);
        }
    }
}
