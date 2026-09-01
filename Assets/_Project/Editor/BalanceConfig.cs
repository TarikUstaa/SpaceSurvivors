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

            // ---- roster timing (iter-5) — the player wants challenge + variety far sooner, so
            //      the "first 3:00 is chaff only" rule is dropped: every archetype is in by ~2:40.
            SetEnemy("Grunt",    earliest: 0f,   weight: 1.0f);
            SetEnemy("Swarmer",  earliest: 15f,  weight: 1.4f);
            SetEnemy("Shooter",  earliest: 40f,  weight: 0.6f);
            SetEnemy("Charger",  earliest: 75f,  weight: 0.6f);
            SetEnemy("Splitter", earliest: 100f, weight: 0.55f);
            SetEnemy("Brute",    earliest: 160f, weight: 0.45f);

            // ---- iter-5: MUCH denser. The old curves peaked at 8-10 enemies/s and the screen
            //      felt thin the whole run. These ramp to bullet-heaven density (20-26/s mid-run)
            //      and the swarm event (EventDirector, every 60s) dumps ~34 more on top. Infinite
            //      still ramps a touch gentler than Campaign so infinite runs last longer. ----
            infinite.spawnRatePerSecond = Curve(
                (0, 0.7f), (20, 1.5f), (45, 2.6f), (90, 4f), (150, 5.8f),
                (300, 8.5f), (480, 12f), (720, 16f), (1080, 21f), (1500, 25f));
            infinite.healthMultiplier = Curve(
                (0, 1f), (60, 1.12f), (150, 1.42f), (180, 1.58f),
                (300, 2.2f), (480, 3.2f), (720, 4.4f), (1080, 6.4f), (1500, 8.8f));
            infinite.speedMultiplier = Curve(
                (0, 1f), (120, 1.04f), (300, 1.1f), (600, 1.18f), (1080, 1.28f), (1500, 1.4f));
            infinite.maxAliveEnemies = 400;
            infinite.bossSchedule = new List<BossEntry>
            {
                Boss(180f, mini, 1, 4f),
                Boss(420f, mini, 2, 4f),
                Boss(720f, mini, 3, 4f),
                Boss(1080f, final, 1, 5f),
                Boss(1500f, final, 2, 5f),
            };

            campaign.spawnRatePerSecond = Curve(
                (0, 0.8f), (20, 1.8f), (45, 3f), (90, 4.8f), (150, 7f),
                (240, 10f), (360, 14f), (540, 18f), (780, 23f), (900, 26f));
            campaign.healthMultiplier = Curve(
                (0, 1f), (60, 1.15f), (150, 1.5f), (180, 1.7f),
                (300, 2.6f), (480, 4f), (720, 5.8f), (900, 7.2f));
            campaign.speedMultiplier = Curve(
                (0, 1f), (120, 1.05f), (180, 1.1f), (400, 1.18f), (700, 1.27f), (900, 1.34f));
            campaign.maxAliveEnemies = 420;
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
