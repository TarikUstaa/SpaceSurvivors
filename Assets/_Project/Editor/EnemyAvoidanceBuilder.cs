using SpaceSurvivors.Enemies;
using UnityEditor;
using UnityEngine;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// M19 — adds the <see cref="ObstacleAvoidance"/> steering modifier to every non-boss enemy
    /// prefab so swarms flow around asteroids and wrecks instead of grinding into them. The probe
    /// is sized from each prefab's own collider. Bosses are left alone — they get to plough
    /// through. Idempotent: re-running just re-applies the tuning.
    /// </summary>
    internal static class EnemyAvoidanceBuilder
    {
        private const string Dir = "Assets/_Project/Prefabs/Enemies/";
        private const int ObstacleLayer = 11;

        // prefab name, look-ahead (world units), push strength (× top speed)
        private static readonly (string name, float lookAhead, float strength)[] Targets =
        {
            ("Grunt",        2.2f, 1.4f),
            ("Shooter",      2.4f, 1.5f),
            ("Charger",      2.0f, 1.1f), // committed dashes should still mostly commit
            ("Splitter",     2.3f, 1.3f),
            ("SplitterMite", 1.6f, 1.3f),
            ("Brute",        2.6f, 1.2f), // heavy: wide berth, gentle turn
        };

        [MenuItem("SpaceSurvivors/Build/M19 Enemy obstacle avoidance")]
        private static void Build()
        {
            int touched = 0;
            foreach (var (name, lookAhead, strength) in Targets)
            {
                string path = Dir + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                {
                    Debug.LogWarning($"[EnemyAvoidanceBuilder] missing prefab: {path}");
                    continue;
                }

                var avoid = root.GetComponent<ObstacleAvoidance>();
                if (avoid == null) avoid = root.AddComponent<ObstacleAvoidance>();

                float probe = 0.45f;
                var circle = root.GetComponent<CircleCollider2D>();
                if (circle != null)
                    probe = circle.radius * root.transform.localScale.x + 0.14f;

                var so = new SerializedObject(avoid);
                so.FindProperty("_lookAhead").floatValue = lookAhead;
                so.FindProperty("_probeRadius").floatValue = probe;
                so.FindProperty("_strength").floatValue = strength;
                so.FindProperty("_obstacleLayers").intValue = 1 << ObstacleLayer;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                touched++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[EnemyAvoidanceBuilder] ObstacleAvoidance applied to {touched} enemy prefabs.");
        }
    }
}
