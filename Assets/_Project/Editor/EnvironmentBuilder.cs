using System.Collections.Generic;
using System.Linq;
using SpaceSurvivors.Combat;
using SpaceSurvivors.Data;
using SpaceSurvivors.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// Builds the M15 environment feature: the Obstacle layer + physics matrix, the obstacle /
    /// cache / hazard / debris prefabs, three <see cref="MapData"/> assets + their catalogue,
    /// and the <c>EnvironmentDirector</c> wiring in <c>Game.unity</c>. Editor tool because the
    /// RunCommand assembly can't build prefabs or touch project settings. Idempotent.
    /// </summary>
    internal static class EnvironmentBuilder
    {
        private const string Art = "Assets/_Project/Art/";
        private const string PrefabDir = "Assets/_Project/Prefabs/Environment/";
        private const string HealthDir = "Assets/_Project/ScriptableObjects/Config/";
        private const string ResDir = "Assets/_Project/Resources/";
        private const int ObstacleLayer = 11;

        private static Sprite Spr(string sub) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + sub);

        // ------------------------------------------------------------------ layers + physics

        [MenuItem("SpaceSurvivors/Build/M15 Setup layers + physics")]
        private static void SetupLayersAndPhysics()
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            var slot = layers.GetArrayElementAtIndex(ObstacleLayer);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = "Obstacle";
                tagManager.ApplyModifiedProperties();
                Debug.Log("[EnvironmentBuilder] added layer 11 = Obstacle");
            }

            // Obstacle collides with Enemy(6), PlayerProjectile(7), Player(9), EnemyProjectile(10)
            // only. Not with itself (kinematic anyway), pickups, or anything else.
            var keep = new HashSet<int> { 6, 7, 9, 10 };
            for (int i = 0; i < 32; i++)
                Physics2D.IgnoreLayerCollision(ObstacleLayer, i, ignore: !keep.Contains(i));

            Debug.Log("[EnvironmentBuilder] physics matrix set for Obstacle layer");
        }

        // ------------------------------------------------------------------ assets

        [MenuItem("SpaceSurvivors/Build/M15 Build environment assets")]
        private static void BuildAssets()
        {
            SetupLayersAndPhysics();
            System.IO.Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();

            var hardHp = MakeHealth("Obstacle_AsteroidHard", 99999f);
            var rockHp = MakeHealth("Obstacle_Asteroid", 46f);
            var cacheHp = MakeHealth("Obstacle_ScrapCache", 30f);

            var debris = BuildDebris();
            var large = BuildObstacle("Env_AsteroidLarge", "Sprites/Base_Assets/Meteors/meteorGrey_big4.png",
                hardHp, colliderRadius: 0.40f, scale: 1.55f, scrap: 0, spin: 6f, hitFlash: false,
                tint: new Color(0.66f, 0.68f, 0.74f));
            var small = BuildObstacle("Env_AsteroidSmall", "Sprites/Base_Assets/Meteors/meteorBrown_big1.png",
                rockHp, colliderRadius: 0.40f, scale: 0.95f, scrap: 0, spin: -18f, hitFlash: true,
                tint: new Color(0.82f, 0.7f, 0.56f));
            BuildCache("Env_ScrapCache", cacheHp);
            BuildHazard("Env_HazardZone");

            BackdropTextureBaker.Bake();
            var catalogue = BuildMaps();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EnvironmentBuilder] assets built. Catalogue has {catalogue.maps.Count} maps.");
        }

        private static HealthData MakeHealth(string name, float hp)
        {
            string path = HealthDir + name + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<HealthData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<HealthData>();
                AssetDatabase.CreateAsset(data, path);
            }
            data.maxHealth = hp;
            data.invulnerabilityAfterHit = 0f;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject BuildObstacle(string name, string sprite, HealthData hp,
            float colliderRadius, float scale, int scrap, float spin, bool hitFlash, Color tint)
        {
            var go = new GameObject(name);
            go.layer = ObstacleLayer;
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Spr(sprite);
            sr.color = tint;
            sr.sortingOrder = -5;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = colliderRadius;

            var health = go.AddComponent<HealthComponent>();
            new SerializedObject(health).Also(so => so.FindProperty("_data").objectReferenceValue = hp);

            if (hitFlash)
            {
                var hf = go.AddComponent<HitFlash>();
                new SerializedObject(hf).Also(so => so.FindProperty("_health").objectReferenceValue = health);
            }

            var ob = go.AddComponent<Obstacle>();
            new SerializedObject(ob).Also(so =>
            {
                so.FindProperty("_scrapReward").intValue = scrap;
                so.FindProperty("_spinSpeed").floatValue = spin;
                so.FindProperty("_driftSpeed").floatValue = 0f;
            });

            return SavePrefab(go);
        }

        private static GameObject BuildCache(string name, HealthData hp)
        {
            var go = new GameObject(name);
            go.layer = ObstacleLayer;
            go.transform.localScale = Vector3.one * 1.7f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Spr("Sprites/Base_Assets/Power-ups/things_silver.png");
            sr.color = new Color(1f, 0.86f, 0.42f); // gold-ish → reads as loot
            sr.sortingOrder = -4;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.30f, 0.30f);

            var health = go.AddComponent<HealthComponent>();
            new SerializedObject(health).Also(so => so.FindProperty("_data").objectReferenceValue = hp);

            var hf = go.AddComponent<HitFlash>();
            new SerializedObject(hf).Also(so => so.FindProperty("_health").objectReferenceValue = health);

            var ob = go.AddComponent<Obstacle>();
            new SerializedObject(ob).Also(so =>
            {
                so.FindProperty("_scrapReward").intValue = 18;
                so.FindProperty("_spinSpeed").floatValue = 30f;
                so.FindProperty("_driftSpeed").floatValue = 0f;
            });

            return SavePrefab(go);
        }

        private static GameObject BuildHazard(string name)
        {
            var go = new GameObject(name);

            var pulseGo = new GameObject("Pulse");
            pulseGo.transform.SetParent(go.transform, false);
            pulseGo.transform.localScale = Vector3.one * 3.9f;
            var psr = pulseGo.AddComponent<SpriteRenderer>();
            psr.sprite = Spr("Sprites/Generated/AuraRing.png");
            psr.color = new Color(1f, 0.42f, 0.20f, 0.32f);
            psr.sortingOrder = -3;

            var hz = go.AddComponent<HazardZone>();
            new SerializedObject(hz).Also(so =>
            {
                so.FindProperty("_radius").floatValue = 2.4f;
                so.FindProperty("_damagePerTick").floatValue = 5f;
                so.FindProperty("_tickInterval").floatValue = 0.6f;
                so.FindProperty("_targetLayers").intValue = (1 << 6) | (1 << 9); // Enemy + Player
                so.FindProperty("_pulse").objectReferenceValue = pulseGo.transform;
                so.FindProperty("_pulseAmount").floatValue = 0.08f;
                so.FindProperty("_pulseSpeed").floatValue = 2.4f;
            });

            return SavePrefab(go);
        }

        private static GameObject BuildDebris()
        {
            var go = new GameObject("Env_Debris");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Spr("Sprites/Base_Assets/Meteors/meteorBrown_med3.png");
            sr.color = new Color(0.85f, 0.72f, 0.55f);
            sr.sortingOrder = 30;

            var pulse = go.AddComponent<OneShotPulse>();
            new SerializedObject(pulse).Also(so =>
            {
                so.FindProperty("_lifetime").floatValue = 0.32f;
                so.FindProperty("_startScale").floatValue = 0.5f;
                so.FindProperty("_endScale").floatValue = 1.7f;
                so.FindProperty("_spinDegrees").floatValue = 220f;
            });

            return SavePrefab(go);
        }

        // ------------------------------------------------------------------ maps

        private static MapCatalogue BuildMaps()
        {
            var dim = new Color(1f, 1f, 1f, 0.6f); // keep the backdrop atmospheric, not blinding

            var milky = MakeMap("milky_way", "Milky Way",
                "The galactic core spills across the sky in a river of pale light.",
                new Color(0.04f, 0.05f, 0.10f), new Color(0.78f, 0.84f, 1f),
                "Backdrop_MilkyWay.png", dim);

            var nebula = MakeMap("crimson_nebula", "Crimson Nebula",
                "A curtain of red gas glows around you — a stellar nursery, still burning.",
                new Color(0.07f, 0.03f, 0.045f), new Color(1f, 0.82f, 0.8f),
                "Backdrop_Nebula.png", dim);

            var nova = MakeMap("supernova", "Supernova",
                "Dying stars tear themselves apart in the dark. Shockwaves ripple past.",
                new Color(0.07f, 0.04f, 0.03f), new Color(1f, 0.86f, 0.7f),
                "Backdrop_Supernova.png", dim);

            string path = ResDir + "MapCatalogue.asset";
            var cat = AssetDatabase.LoadAssetAtPath<MapCatalogue>(path);
            if (cat == null)
            {
                cat = ScriptableObject.CreateInstance<MapCatalogue>();
                AssetDatabase.CreateAsset(cat, path);
            }
            cat.maps = new List<MapData> { milky, nebula, nova };
            EditorUtility.SetDirty(cat);

            // Retire the old gameplay-per-map assets.
            foreach (var old in new[] { "open_space", "asteroid_belt", "ion_storm" })
                AssetDatabase.DeleteAsset(ResDir + "Map_" + old + ".asset");

            return cat;
        }

        private static MapData MakeMap(string id, string name, string desc, Color bg, Color starTint,
            string backdrop, Color backdropTint)
        {
            string path = ResDir + "Map_" + id + ".asset";
            var map = AssetDatabase.LoadAssetAtPath<MapData>(path);
            if (map == null)
            {
                map = ScriptableObject.CreateInstance<MapData>();
                AssetDatabase.CreateAsset(map, path);
            }
            map.id = id;
            map.displayName = name;
            map.description = desc;
            map.cameraBackground = bg;
            map.starfieldTint = starTint;
            map.backdropSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/Sprites/Generated/" + backdrop);
            map.backdropTint = backdropTint;
            EditorUtility.SetDirty(map);
            return map;
        }

        // ------------------------------------------------------------------ scene wiring

        [MenuItem("SpaceSurvivors/Build/M15 Wire Game scene")]
        private static void WireGameScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Game.unity", OpenSceneMode.Single);

            var pool = Object.FindAnyObjectByType<SpaceSurvivors.Core.PoolManager>();
            var move = Object.FindAnyObjectByType<SpaceSurvivors.Player.PlayerMovement>();
            var player = move != null ? move.gameObject : null;
            var cam = Camera.main;
            var collector = Object.FindAnyObjectByType<SpaceSurvivors.Progression.ScrapCollector>();
            var starfield = Object.FindAnyObjectByType<SpaceSurvivors.Core.StarfieldParallax>();
            var systems = pool != null ? pool.gameObject : GameObject.Find("Systems");

            if (systems == null || player == null)
            {
                Debug.LogError($"[EnvironmentBuilder] not found: systems={systems}, player={player}, pool={pool}");
                return;
            }

            var dir = systems.GetComponent<EnvironmentDirector>();
            if (dir == null) dir = systems.AddComponent<EnvironmentDirector>();

            var debris = Load("Env_Debris");
            var scrapPickup = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Pickups/ScrapPickup.prefab");
            var fallbackMap = AssetDatabase.LoadAssetAtPath<MapData>(ResDir + "Map_milky_way.asset");

            var so = new SerializedObject(dir);
            so.FindProperty("_pool").objectReferenceValue = pool;
            so.FindProperty("_player").objectReferenceValue = player.transform;
            so.FindProperty("_camera").objectReferenceValue = cam;
            so.FindProperty("_collector").objectReferenceValue = collector;
            so.FindProperty("_starfield").objectReferenceValue = starfield;
            so.FindProperty("_debrisVfxPrefab").objectReferenceValue = debris;
            so.FindProperty("_scrapPickupPrefab").objectReferenceValue = scrapPickup;
            so.FindProperty("_fallbackMap").objectReferenceValue = fallbackMap;
            so.FindProperty("_ringRadius").intValue = 3;
            so.FindProperty("_propsPerCell").intValue = 5;
            so.FindProperty("_cellSize").floatValue = 14f;
            so.FindProperty("_spawnClearRadius").floatValue = 5f;

            // The standard arena field — the same on every map (asteroids are core gameplay,
            // not a backdrop feature).
            var field = new (string name, float weight)[]
            {
                ("Env_AsteroidLarge", 2f), ("Env_AsteroidSmall", 4.5f),
                ("Env_ScrapCache", 1f), ("Env_HazardZone", 0.7f),
            };
            var props = so.FindProperty("_props");
            props.arraySize = field.Length;
            for (int i = 0; i < field.Length; i++)
            {
                var e = props.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("prefab").objectReferenceValue = Load(field[i].name);
                e.FindPropertyRelative("weight").floatValue = field[i].weight;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // Player: give PlayerMovement the Obstacle mask so the kinematic ship is deflected.
            if (move != null)
            {
                var pso = new SerializedObject(move);
                pso.FindProperty("_obstacleMask").intValue = 1 << ObstacleLayer;
                pso.ApplyModifiedPropertiesWithoutUndo();
            }

            // Prewarm pools for the streamed props.
            AddPrewarm(pool, new (string, int)[]
            {
                ("Env_AsteroidLarge", 110), ("Env_AsteroidSmall", 210), ("Env_ScrapCache", 55),
                ("Env_HazardZone", 120), ("Env_Debris", 12),
            });

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EnvironmentBuilder] wired EnvironmentDirector into Game.unity");
        }

        private static void AddPrewarm(SpaceSurvivors.Core.PoolManager pool, (string name, int count)[] entries)
        {
            if (pool == null) return;
            var so = new SerializedObject(pool);
            var list = so.FindProperty("_prewarm");
            foreach (var (name, count) in entries)
            {
                var prefab = Load(name);
                if (prefab == null) continue;

                SerializedProperty slot = null;
                for (int i = 0; i < list.arraySize; i++)
                {
                    var e = list.GetArrayElementAtIndex(i);
                    var p = e.FindPropertyRelative("prefab").objectReferenceValue;
                    if (p != null && p.name == name) { slot = e; break; }
                }
                if (slot == null)
                {
                    list.arraySize++;
                    slot = list.GetArrayElementAtIndex(list.arraySize - 1);
                    slot.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                }
                slot.FindPropertyRelative("count").intValue = count;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ helpers

        private static GameObject SavePrefab(GameObject go)
        {
            string path = PrefabDir + go.name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject Load(string name)
            => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + name + ".prefab");

        private static void Also(this SerializedObject so, System.Action<SerializedObject> edit)
        {
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
