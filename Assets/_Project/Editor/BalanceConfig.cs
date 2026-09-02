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
        private const string Upg = "Assets/_Project/ScriptableObjects/Upgrades/";

        [MenuItem("SpaceSurvivors/Balance/M16 Apply balance")]
        private static void Apply()
        {
            var infinite = Load<DifficultyConfig>(Cfg + "DifficultyConfig.asset");
            var campaign = Load<DifficultyConfig>(Cfg + "CampaignDifficulty.asset");
            var mini = Load<EnemyData>(Enm + "MiniBoss.asset");
            var final = Load<EnemyData>(Enm + "FinalBoss.asset");

            // ---- roster timing (iter-6) — every archetype in by ~2:40. Shooter promoted from
            //      a rare 0.6 to a full 1.0: ranged fire is the main thing that punishes a
            //      player kiting in a straight line, and the sim showed straight-line kiting
            //      was risk-free. Swarmer (the fast chaser) also up a touch. ----
            SetEnemy("Grunt",    earliest: 0f,   weight: 1.0f);
            SetEnemy("Swarmer",  earliest: 25f,  weight: 1.3f);
            SetEnemy("Shooter",  earliest: 40f,  weight: 0.9f);
            SetEnemy("Charger",  earliest: 80f,  weight: 0.7f);
            SetEnemy("Splitter", earliest: 110f, weight: 0.55f);
            SetEnemy("Brute",    earliest: 170f, weight: 0.45f);

            // ---- enemy base move speed (iter-6). Everything was ~2 u/s vs the player's 6, so
            //      kiting was free. Bring the chasers up so an un-invested player gets caught
            //      late-run (the DifficultyConfig speedMultiplier then ramps this further). ----
            SetEnemySpeed("Grunt",    2.6f);
            SetEnemySpeed("Swarmer",  4.0f);
            SetEnemySpeed("Shooter",  2.7f);
            SetEnemySpeed("Charger",  2.3f);
            SetEnemySpeed("Splitter", 2.1f);
            SetEnemySpeed("Brute",    1.25f);

            // ---- iter-6c: density + threat pass. iter-6b fixed the first ~3 min (swarms are a
            //      real threat) but past 3:00 a snowballed build's kill rate (~12/s) still
            //      outran the spawn rate in the lulls between swarms — HP pinned at max again.
            //      iter-6c keeps 0:00–2:00 identical to 6b, then pushes the mid/late curve much
            //      harder: ~11/s by 3:00, 22/s by 8:00, 44/s by 18:00, 70/s by 40:00; HP scales
            //      to ×22 late; maxAlive 550→600. (Swarm size also up — see EnvironmentEventsBuilder.)
            //      Infinite still ramps gentler than Campaign so infinite runs last longer. ----
            infinite.spawnRatePerSecond = Curve(
                (0, 0.5f), (25, 1f), (60, 1.8f), (120, 3.2f), (180, 5.5f),
                (260, 9f), (360, 15f), (480, 22f), (720, 32f), (1080, 44f), (1500, 55f), (2400, 70f));
            infinite.healthMultiplier = Curve(
                (0, 1f), (60, 1.12f), (180, 1.7f), (300, 2.6f), (480, 4f),
                (700, 6f), (1000, 9f), (1500, 13f), (2400, 22f));
            infinite.speedMultiplier = Curve(
                (0, 1f), (60, 1.03f), (180, 1.1f), (360, 1.22f),
                (600, 1.36f), (900, 1.5f), (1500, 1.7f));
            infinite.maxAliveEnemies = 600;
            infinite.bossSchedule = new List<BossEntry>
            {
                Boss(180f, mini, 1, 4f),
                Boss(420f, mini, 2, 4f),
                Boss(720f, mini, 3, 4f),
                Boss(1080f, final, 1, 5f),
                Boss(1500f, final, 2, 5f),
            };

            campaign.spawnRatePerSecond = Curve(
                (0, 0.7f), (25, 1.4f), (60, 2.5f), (120, 4f), (180, 6.5f),
                (260, 10f), (360, 17f), (500, 26f), (700, 35f), (900, 42f));
            campaign.healthMultiplier = Curve(
                (0, 1f), (60, 1.18f), (150, 1.7f), (180, 2f), (300, 3f),
                (450, 4.5f), (650, 6.5f), (900, 9f));
            campaign.speedMultiplier = Curve(
                (0, 1f), (60, 1.04f), (180, 1.13f), (360, 1.28f), (600, 1.42f), (900, 1.58f));
            campaign.maxAliveEnemies = 520;
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

            // ---- in-run upgrade balance (G3, 2026-09-01) — weight / per-stack value / stack
            //      ceiling for every level-up choice, in one place. Stack ceilings on evolution
            //      catalysts double as "picks to evolve" (see the // -> notes). ----
            TuneUpgrades();

            foreach (var o in new Object[] { infinite, campaign, prog }) EditorUtility.SetDirty(o);
            AssetDatabase.SaveAssets();
            Debug.Log("[BalanceConfig] applied balance (curves + roster + bosses + XP + G3 upgrades).");
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

        private static void SetEnemySpeed(string name, float moveSpeed)
        {
            var e = Load<EnemyData>(Enm + name + ".asset");
            if (e == null) return;
            var so = new SerializedObject(e);
            so.FindProperty("moveSpeed").floatValue = moveSpeed;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(e);
        }

        /// <summary>
        /// G3 upgrade pass. Rationale per line; the headline moves:
        ///  • FireRate was the runaway passive (multiplies weapon + mine cadence) — 6→5 stacks, +20→18%.
        ///  • MoveSpeed fed the kiting snowball and gated the Mine evolution at an unreachable 6 — 6→4.
        ///  • Haste (projectile speed) is a near-dead stat only wanted as the VoidLance catalyst —
        ///    weight 0.9→0.5, 5→3 stacks so committing to the Rail path actually pays off.
        ///  • MaxHealth per-stack 25→30 (G4's denser swarms hit harder), ceiling 6→5.
        ///  • MultiShot was almost never offered (w0.35) despite being a fair pick — 0.35→0.5.
        ///  • Weapon grants nudged just below the passives so mid-run isn't all "new gun" cards;
        ///    with the new WeaponController slot cap you pick ~5 of 7 anyway.
        /// </summary>
        private static void TuneUpgrades()
        {
            //         name            weight  maxStacks  modValue   (// evolution note)
            SetUpgrade("Damage",        1.00f,  5,  0.25f);              // Laser → Prism  @5
            SetUpgrade("FireRate",      0.85f,  5,  0.18f, "+18% fire rate");        // PlasmaOrb → Nova Core @5
            SetUpgrade("MaxHealth",     0.95f,  5,  30f,   "+30 max HP (heals too)"); // Static Field → Ion Storm @5
            SetUpgrade("MoveSpeed",     0.85f,  4,  0.10f, "+10% move speed");       // Mine Layer → Deep Mine @4
            SetUpgrade("MultiShot",     0.50f,  3,  1f);                 // Missile → Cluster Missile @3
            SetUpgrade("Pierce",        0.80f,  4,  1f);                 // Scatter Shot → Buckshot Storm @4
            SetUpgrade("Haste",         0.50f,  3,  0.15f);             // Rail Spike → Void Lance @3
            SetUpgrade("PickupRadius",  0.55f,  3,  0.30f);             // Orbiter → Event Horizon @3
            SetUpgrade("Shield",        0.90f,  4,  1f);

            SetUpgrade("GetMissiles",    0.80f, 1);
            SetUpgrade("GetPlasmaOrb",   0.70f, 1);
            SetUpgrade("GetScatterShot", 0.70f, 1);
            SetUpgrade("GetRailSpike",   0.70f, 1);
            SetUpgrade("GetOrbiter",     0.70f, 1);
            SetUpgrade("GetStaticField", 0.70f, 1);
            SetUpgrade("GetMineLayer",   0.55f, 1);   // slated for removal in G5
        }

        private static void SetUpgrade(string name, float weight, int maxStacks,
                                       float? modValue = null, string desc = null)
        {
            var u = AssetDatabase.LoadAssetAtPath<UpgradeData>(Upg + name + ".asset");
            if (u == null) { Debug.LogWarning($"[BalanceConfig] no upgrade '{name}'"); return; }
            var so = new SerializedObject(u);
            so.FindProperty("weight").floatValue = weight;
            so.FindProperty("maxStacks").intValue = maxStacks;
            if (modValue.HasValue)
            {
                var mods = so.FindProperty("modifiers");
                if (mods.arraySize > 0)
                    mods.GetArrayElementAtIndex(0).FindPropertyRelative("value").floatValue = modValue.Value;
            }
            if (!string.IsNullOrEmpty(desc)) so.FindProperty("description").stringValue = desc;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(u);
        }
    }
}
