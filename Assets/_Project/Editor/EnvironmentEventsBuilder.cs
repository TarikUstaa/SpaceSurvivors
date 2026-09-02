using System;
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
    /// M18 — environment enrichment. Builds the extra arena props (wreck / debris / crystal /
    /// drift-mine / bonus-pod), the five space events + their FX prefabs, the
    /// <c>SpaceEventCatalogue</c>, a 6-map <c>MapCatalogue</c> on the CC0 SBS nebula
    /// backgrounds, and wires the <c>EventDirector</c> + <c>EventBanner</c> into Game.unity.
    /// Editor tool (the RunCommand assembly can't build prefabs / touch importers). Idempotent.
    /// Menu: SpaceSurvivors/Build/M18 Environment + Events.
    /// </summary>
    internal static class EnvironmentEventsBuilder
    {
        private const string Art = "Assets/_Project/Art/";
        private const string Ext = Art + "Sprites/Extension_Assets/Sprites/";
        private const string Bg = Art + "Sprites/Backgrounds/";
        private const string PrefabDir = "Assets/_Project/Prefabs/Environment/";
        private const string EventDir = "Assets/_Project/Prefabs/Events/";
        private const string HealthDir = "Assets/_Project/ScriptableObjects/Config/";
        private const string ResDir = "Assets/_Project/Resources/";
        private const string GameScene = "Assets/_Project/Scenes/Game.unity";
        private const string GlowMat = Art + "Particles/WeaponGlow.mat";
        private const int ObstacleLayer = 11;

        [MenuItem("SpaceSurvivors/Build/M18 Environment + Events")]
        public static void Build()
        {
            ImportBackgrounds();
            System.IO.Directory.CreateDirectory(EventDir);

            BuildProps();
            BuildEventFx();
            BuildEventPrefabs();
            var eventCat = BuildEventCatalogue();
            BuildMaps();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WireScene();

            Debug.Log($"[EnvironmentEventsBuilder] M18 built. {eventCat.events.Count} events.");
        }

        // ---------------------------------------------------------------- backgrounds

        private static void ImportBackgrounds()
        {
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { Bg.TrimEnd('/') })
                         .Select(AssetDatabase.GUIDToAssetPath))
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter imp) continue;
                bool dirty = false;
                if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; dirty = true; }
                if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                // PPU sets how much of the 1024² tile fills the screen. Too low (7) and the
                // camera sees ~12% of the image blown up 8× → pixel mush. ~32 shows about half
                // the tile per screen: sharp enough, tile repeat ≈ 2 screen-widths (gentle).
                if (!Mathf.Approximately(imp.spritePixelsPerUnit, 32f)) { imp.spritePixelsPerUnit = 32f; dirty = true; }
                if (imp.wrapMode != TextureWrapMode.Repeat) { imp.wrapMode = TextureWrapMode.Repeat; dirty = true; }
                if (imp.filterMode != FilterMode.Bilinear) { imp.filterMode = FilterMode.Bilinear; dirty = true; }
                if (!imp.mipmapEnabled) { imp.mipmapEnabled = true; dirty = true; }
                if (imp.maxTextureSize < 2048) { imp.maxTextureSize = 2048; dirty = true; }
                // Normal compression blocks up the smooth nebula gradients — use HQ.
                if (imp.textureCompression != TextureImporterCompression.CompressedHQ)
                { imp.textureCompression = TextureImporterCompression.CompressedHQ; dirty = true; }

                // Tiled draw mode needs a Full Rect mesh or the edges clip.
                var st = new TextureImporterSettings();
                imp.ReadTextureSettings(st);
                if (st.spriteMeshType != SpriteMeshType.FullRect)
                {
                    st.spriteMeshType = SpriteMeshType.FullRect;
                    imp.SetTextureSettings(st);
                    dirty = true;
                }
                if (dirty) imp.SaveAndReimport();
            }
        }

        // ---------------------------------------------------------------- props

        private static void BuildProps()
        {
            var hardHp = MakeHealth("Obstacle_WreckHard", 99999f);
            var debrisHp = MakeHealth("Obstacle_DebrisChunk", 22f);
            var crystalHp = MakeHealth("Obstacle_Crystal", 44f);

            // Big derelict station — indestructible cover.
            BuildObstacle("Env_Wreck", Ext + "Station/spaceStation_018.png", hardHp,
                box: new Vector2(0.9f, 0.32f), scale: 1.7f, scrap: 0, spin: 2f, drift: 0f,
                hitFlash: false, sortOrder: -5, tint: new Color(0.52f, 0.55f, 0.60f));

            // Drifting hull fragment — cracks for a little scrap.
            BuildObstacle("Env_DebrisChunk", Ext + "Parts/spaceParts_016.png", debrisHp,
                box: new Vector2(0.28f, 0.34f), scale: 0.55f, scrap: 6, spin: -40f, drift: 0.35f,
                hitFlash: true, sortOrder: -4, tint: new Color(0.5f, 0.53f, 0.58f));

            // Crystal formation — tougher, pays out well.
            BuildObstacle("Env_Crystal", "Sprites/Base_Assets/Meteors/meteorGrey_med1.png", crystalHp,
                circle: 0.34f, scale: 0.9f, scrap: 26, spin: 22f, drift: 0f,
                hitFlash: true, sortOrder: -4, tint: new Color(0.55f, 0.9f, 1f));

            BuildDriftMine();
            BuildBonusPod();
        }

        private static void BuildObstacle(string name, string sprite, HealthData hp,
            float scale, int scrap, float spin, float drift, bool hitFlash, int sortOrder, Color tint,
            float circle = 0f, Vector2 box = default)
        {
            var go = new GameObject(name) { layer = ObstacleLayer };
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Spr(sprite);
            sr.color = tint;
            sr.sortingOrder = sortOrder;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            if (box != default) go.AddComponent<BoxCollider2D>().size = box;
            else go.AddComponent<CircleCollider2D>().radius = circle > 0f ? circle : 0.35f;

            var health = go.AddComponent<HealthComponent>();
            new SerializedObject(health).Do(so => so.FindProperty("_data").objectReferenceValue = hp);

            if (hitFlash)
            {
                var hf = go.AddComponent<HitFlash>();
                new SerializedObject(hf).Do(so => so.FindProperty("_health").objectReferenceValue = health);
            }

            var ob = go.AddComponent<Obstacle>();
            new SerializedObject(ob).Do(so =>
            {
                so.FindProperty("_scrapReward").intValue = scrap;
                so.FindProperty("_spinSpeed").floatValue = spin;
                so.FindProperty("_driftSpeed").floatValue = drift;
            });

            Save(go, PrefabDir);
        }

        private static void BuildDriftMine()
        {
            var go = new GameObject("Env_DriftMine") { layer = ObstacleLayer };
            go.transform.localScale = Vector3.one * 0.5f;

            // A dark spiked body reads as "hazard", not "rock".
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Spr("Sprites/Base_Assets/Meteors/meteorGrey_small1.png");
            sr.color = new Color(0.5f, 0.2f, 0.2f);
            sr.sortingOrder = 3;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.55f;

            // Wide soft red danger halo (behind the core).
            var haloGo = new GameObject("Halo");
            haloGo.transform.SetParent(go.transform, false);
            haloGo.transform.localScale = Vector3.one * 2.7f;
            var hsr = haloGo.AddComponent<SpriteRenderer>();
            hsr.sprite = P("WeaponOrbGlow");
            hsr.sharedMaterial = Mat();
            hsr.color = new Color(1f, 0.12f, 0.08f, 0.42f);
            hsr.sortingOrder = 2;

            // Bright pulsing warning light (in front).
            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localScale = Vector3.one * 1.7f;
            var lsr = lightGo.AddComponent<SpriteRenderer>();
            lsr.sprite = P("star_05");
            lsr.sharedMaterial = Mat();
            lsr.color = new Color(1f, 0.2f, 0.15f, 1f);
            lsr.sortingOrder = 4;

            var mine = go.AddComponent<DriftMine>();
            new SerializedObject(mine).Do(so =>
            {
                so.FindProperty("_blastVfxPrefab").objectReferenceValue = LoadAny("Assets/_Project/Prefabs/Projectiles/MissileImpact.prefab");
                so.FindProperty("_light").objectReferenceValue = lsr;
            });

            Save(go, PrefabDir);
        }

        private static void BuildBonusPod()
        {
            var go = new GameObject("Env_BonusPod");
            go.transform.localScale = Vector3.one * 0.5f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Spr(Ext + "Parts/spaceParts_040.png");
            sr.color = new Color(0.55f, 0.9f, 1f);
            sr.sortingOrder = -3;

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(go.transform, false);
            glowGo.transform.localScale = Vector3.one * 2.2f;
            var gsr = glowGo.AddComponent<SpriteRenderer>();
            gsr.sprite = P("WeaponOrbGlow");
            gsr.sharedMaterial = Mat();
            gsr.color = new Color(0.4f, 0.9f, 1f, 0.5f);
            gsr.sortingOrder = -4;

            go.AddComponent<CircleCollider2D>().radius = 0.7f;

            var pod = go.AddComponent<BonusPod>();
            new SerializedObject(pod).Do(so =>
            {
                so.FindProperty("_scrapPickupPrefab").objectReferenceValue = LoadAny("Assets/_Project/Prefabs/Pickups/ScrapPickup.prefab");
                so.FindProperty("_popVfxPrefab").objectReferenceValue = LoadAny("Assets/_Project/Prefabs/Pickups/HealBurst.prefab");
            });

            Save(go, PrefabDir);
        }

        // ---------------------------------------------------------------- event FX

        private static void BuildEventFx()
        {
            // Full-screen ion crackle overlay.
            var ion = new GameObject("FX_IonOverlay");
            ion.transform.localScale = Vector3.one * 46f;
            var isr = ion.AddComponent<SpriteRenderer>();
            isr.sprite = P("spark_05");
            isr.sharedMaterial = Mat();
            isr.color = new Color(0.4f, 0.75f, 1f, 0.10f);
            isr.sortingOrder = 40;
            var spin = ion.AddComponent<VfxSpinPulse>();
            new SerializedObject(spin).Do(so =>
            {
                so.FindProperty("_spinDegreesPerSecond").floatValue = 8f;
                so.FindProperty("_pulseAmount").floatValue = 0.06f;
                so.FindProperty("_pulseFrequency").floatValue = 1.4f;
            });
            Save(ion, EventDir);

            // Sweeping solar-flare band (stretched by the event).
            var band = new GameObject("FX_FlareBand");
            var bsr = band.AddComponent<SpriteRenderer>();
            bsr.sprite = P("WeaponOrbGlow");
            bsr.sharedMaterial = Mat();
            bsr.color = new Color(1f, 0.85f, 0.55f, 0f);
            bsr.sortingOrder = 38;
            Save(band, EventDir);

            // Wormhole portal.
            var hole = new GameObject("FX_Wormhole");
            hole.transform.localScale = Vector3.one * 2.4f;
            var hsr = hole.AddComponent<SpriteRenderer>();
            hsr.sprite = P("twirl_02");
            hsr.sharedMaterial = Mat();
            hsr.color = new Color(0.6f, 0.35f, 1f, 0.95f);
            hsr.sortingOrder = 12;
            var hspin = hole.AddComponent<VfxSpinPulse>();
            new SerializedObject(hspin).Do(so =>
            {
                so.FindProperty("_spinDegreesPerSecond").floatValue = -220f;
                so.FindProperty("_pulseAmount").floatValue = 0.1f;
                so.FindProperty("_pulseFrequency").floatValue = 2f;
            });
            Save(hole, EventDir);
        }

        // ---------------------------------------------------------------- event prefabs

        private static void BuildEventPrefabs()
        {
            var meteor = LoadAny(PrefabDir + "Env_AsteroidSmall.prefab");
            var wreck = LoadAny(PrefabDir + "Env_Wreck.prefab");
            var cache = LoadAny(PrefabDir + "Env_ScrapCache.prefab");
            var grunt = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Project/ScriptableObjects/Enemies/Grunt.asset");
            var scrapPickup = LoadAny("Assets/_Project/Prefabs/Pickups/ScrapPickup.prefab");
            var impact = LoadAny("Assets/_Project/Prefabs/Projectiles/LaserImpact.prefab");
            var flash = LoadAny("Assets/_Project/Prefabs/Projectiles/MissileImpact.prefab");

            MakeEvent<MeteorShowerEvent>("Event_MeteorShower", so =>
                so.FindProperty("_meteorPrefab").objectReferenceValue = meteor);

            MakeEvent<IonStormEvent>("Event_IonStorm", so =>
            {
                so.FindProperty("_overlayPrefab").objectReferenceValue = LoadAny(EventDir + "FX_IonOverlay.prefab");
                so.FindProperty("_zapVfxPrefab").objectReferenceValue = impact;
                so.FindProperty("_enemyLayers").intValue = 1 << 6;   // Enemy
            });

            MakeEvent<DerelictConvoyEvent>("Event_DerelictConvoy", so =>
            {
                so.FindProperty("_wreckPrefab").objectReferenceValue = wreck;
                so.FindProperty("_cachePrefab").objectReferenceValue = cache;
                so.FindProperty("_guard").objectReferenceValue = grunt;
            });

            MakeEvent<SolarFlareEvent>("Event_SolarFlare", so =>
            {
                so.FindProperty("_bandPrefab").objectReferenceValue = LoadAny(EventDir + "FX_FlareBand.prefab");
                so.FindProperty("_targetLayers").intValue = (1 << 6) | (1 << 9);   // Enemy + Player
            });

            MakeEvent<WormholeEvent>("Event_Wormhole", so =>
            {
                so.FindProperty("_portalPrefab").objectReferenceValue = LoadAny(EventDir + "FX_Wormhole.prefab");
                so.FindProperty("_exitVfxPrefab").objectReferenceValue = flash;
                so.FindProperty("_scrapPickupPrefab").objectReferenceValue = scrapPickup;
            });

            // G1 — swarm: a wall of fast chaff (+ a little spice) pours in from the edges.
            EnemyData Roster(string n) =>
                AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Project/ScriptableObjects/Enemies/" + n + ".asset");
            var swarmRoster = new[]
            {
                Roster("Grunt"), Roster("Grunt"), Roster("Grunt"),
                Roster("Swarmer"), Roster("Swarmer"),
                Roster("Charger"), Roster("Shooter"),
            };
            MakeEvent<SwarmEvent>("Event_Swarm", so =>
            {
                var rp = so.FindProperty("_roster");
                rp.arraySize = swarmRoster.Length;
                for (int i = 0; i < swarmRoster.Length; i++)
                    rp.GetArrayElementAtIndex(i).objectReferenceValue = swarmRoster[i];
                // Scales with run time (iter-6c): ~30 at minute 1, ~160 at minute 10, cap 180.
                so.FindProperty("_baseEnemies").intValue = 30;
                so.FindProperty("_enemiesPerMinute").floatValue = 13f;
                so.FindProperty("_maxEnemies").intValue = 180;
            });
        }

        private static void MakeEvent<T>(string name, Action<SerializedObject> configure) where T : Component
        {
            var go = new GameObject(name);
            go.AddComponent<SpaceSurvivors.Core.PoolHandle>();
            var comp = go.AddComponent<T>();
            new SerializedObject(comp).Do(configure);
            Save(go, EventDir);
        }

        private static SpaceEventCatalogue BuildEventCatalogue()
        {
            var cat = LoadOrCreate<SpaceEventCatalogue>(ResDir + "SpaceEventCatalogue.asset");
            cat.events = new List<SpaceEventData>
            {
                MakeEventData("meteor_shower", "Meteor Shower", "☄  Meteor shower incoming",
                    weight: 1.4f, earliest: 45f, duration: 22f, "Event_MeteorShower"),
                MakeEventData("ion_storm", "Ion Storm", "⚡  Ion storm — the void crackles",
                    weight: 1f, earliest: 90f, duration: 15f, "Event_IonStorm"),
                MakeEventData("derelict_convoy", "Derelict Convoy", "◈  A derelict convoy drifts near",
                    weight: 0.9f, earliest: 75f, duration: 26f, "Event_DerelictConvoy"),
                MakeEventData("solar_flare", "Solar Flare", "☀  Solar flare — find cover",
                    weight: 0.8f, earliest: 150f, duration: 9f, "Event_SolarFlare"),
                MakeEventData("wormhole", "Wormhole", "◉  A wormhole tears open",
                    weight: 0.7f, earliest: 120f, duration: 20f, "Event_Wormhole"),
            };
            // The swarm is NOT in the random rotation — it runs on EventDirector's fixed
            // 45s track. Its SpaceEventData still needs to exist for that reference.
            MakeEventData("swarm", "Swarm", "⚠  Swarm incoming — brace",
                weight: 0f, earliest: 60f, duration: 20f, "Event_Swarm");
            EditorUtility.SetDirty(cat);
            return cat;
        }

        private static SpaceEventData MakeEventData(string id, string name, string announce,
            float weight, float earliest, float duration, string prefabName)
        {
            var d = LoadOrCreate<SpaceEventData>(HealthDir.Replace("Config/", "Events/") + "Event_" + id + ".asset");
            d.id = id;
            d.displayName = name;
            d.announce = announce;
            d.weight = weight;
            d.earliestTime = earliest;
            d.duration = duration;
            d.eventPrefab = LoadAny(EventDir + prefabName + ".prefab");
            EditorUtility.SetDirty(d);
            return d;
        }

        // ---------------------------------------------------------------- maps

        private static void BuildMaps()
        {
            // Each map now leans hard on THREE distinct signals so they read as different
            // places at a glance: a strongly-hued (but dark) camera clear colour, a bold
            // nebula at ~0.5 alpha, and a dimmed starfield tinted to match so the nebula —
            // not our own stars — carries the mood.
            var maps = new List<MapData>
            {
                Map("milky_way", "Milky Way", "Cold blue arms of the galactic disc drift past.",
                    "Blue_Nebula_05", camBg: new Color(0.035f, 0.055f, 0.13f),
                    starTint: new Color(0.5f, 0.58f, 0.78f), backdropTint: A(1f, 1f, 1f, 0.5f), signatureEvent: ""),

                Map("crimson_nebula", "Crimson Nebula", "A curtain of hot gas glows around the arena.",
                    "Purple_Nebula_04", camBg: new Color(0.13f, 0.03f, 0.06f),
                    starTint: new Color(0.72f, 0.42f, 0.5f), backdropTint: A(1f, 0.55f, 0.6f, 0.55f),
                    signatureEvent: "solar_flare"),

                Map("supernova", "Supernova", "Dying stars tear themselves apart. Shockwaves ripple past.",
                    "Purple_Nebula_07", camBg: new Color(0.14f, 0.06f, 0.03f),
                    starTint: new Color(0.85f, 0.6f, 0.45f), backdropTint: A(1f, 0.62f, 0.35f, 0.55f),
                    signatureEvent: "solar_flare"),

                Map("ion_nebula", "Ion Nebula", "Charged clouds spark and hiss. The static never stops.",
                    "Blue_Nebula_02", camBg: new Color(0.02f, 0.1f, 0.12f),
                    starTint: new Color(0.4f, 0.8f, 0.82f), backdropTint: A(0.7f, 1f, 1f, 0.55f),
                    signatureEvent: "ion_storm"),

                Map("derelict_graveyard", "Derelict Graveyard", "A dead fleet's remains hang in toxic green haze.",
                    "Green_Nebula_03", camBg: new Color(0.04f, 0.11f, 0.06f),
                    starTint: new Color(0.5f, 0.72f, 0.5f), backdropTint: A(0.75f, 1f, 0.78f, 0.55f),
                    signatureEvent: "derelict_convoy"),

                Map("deep_void", "Deep Void", "Almost nothing out here. Almost.",
                    "Starfield_03", camBg: new Color(0.015f, 0.015f, 0.028f),
                    starTint: new Color(0.55f, 0.58f, 0.7f), backdropTint: A(0.75f, 0.78f, 0.95f, 0.32f),
                    signatureEvent: "wormhole"),
            };

            var cat = LoadOrCreate<MapCatalogue>(ResDir + "MapCatalogue.asset");
            cat.maps = maps;
            EditorUtility.SetDirty(cat);
        }

        private static Color A(float r, float g, float b, float a) => new Color(r, g, b, a);

        private static MapData Map(string id, string name, string desc, string bgSprite,
            Color camBg, Color starTint, Color backdropTint, string signatureEvent)
        {
            var m = LoadOrCreate<MapData>(ResDir + "Map_" + id + ".asset");
            m.id = id;
            m.displayName = name;
            m.description = desc;
            m.cameraBackground = camBg;
            m.starfieldTint = starTint;
            m.backdropSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Bg + bgSprite + ".png");
            m.backdropTint = backdropTint;
            m.signatureEventId = signatureEvent;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------------------------------------------------------------- scene wiring

        private static void WireScene()
        {
            var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

            var pool = UnityEngine.Object.FindFirstObjectByType<SpaceSurvivors.Core.PoolManager>();
            var systems = pool != null ? pool.gameObject : GameObject.Find("Systems");
            var envDir = UnityEngine.Object.FindFirstObjectByType<EnvironmentDirector>();
            var starfield = UnityEngine.Object.FindFirstObjectByType<SpaceSurvivors.Core.StarfieldParallax>();
            var move = UnityEngine.Object.FindFirstObjectByType<SpaceSurvivors.Player.PlayerMovement>();
            var clock = UnityEngine.Object.FindFirstObjectByType<SpaceSurvivors.Core.RunClock>();
            var spawns = UnityEngine.Object.FindFirstObjectByType<SpaceSurvivors.Enemies.SpawnDirector>();
            var collector = UnityEngine.Object.FindFirstObjectByType<SpaceSurvivors.Progression.ScrapCollector>();
            if (systems == null || move == null) { Debug.LogError("[M18] systems/player not found"); return; }

            // 1. EventDirector on Systems.
            var ed = systems.GetComponent<EventDirector>() ?? systems.AddComponent<EventDirector>();
            new SerializedObject(ed).Do(so =>
            {
                so.FindProperty("_clock").objectReferenceValue = clock;
                so.FindProperty("_player").objectReferenceValue = move.transform;
                so.FindProperty("_pool").objectReferenceValue = pool;
                so.FindProperty("_spawns").objectReferenceValue = spawns;
                so.FindProperty("_collector").objectReferenceValue = collector;
                so.FindProperty("_camera").objectReferenceValue = Camera.main;
                so.FindProperty("_catalogue").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<SpaceEventCatalogue>(ResDir + "SpaceEventCatalogue.asset");
                so.FindProperty("_swarmEvent").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<SpaceEventData>(
                        HealthDir.Replace("Config/", "Events/") + "Event_swarm.asset");
                so.FindProperty("_firstSwarmAt").floatValue = 60f;
                so.FindProperty("_swarmInterval").floatValue = 45f;
            });

            // 2. Extend the streamed prop table + prewarm.
            if (envDir != null)
            {
                var so = new SerializedObject(envDir);
                var props = so.FindProperty("_props");
                var want = new (string name, float weight)[]
                {
                    ("Env_AsteroidLarge", 2f), ("Env_AsteroidSmall", 4f), ("Env_ScrapCache", 1f),
                    ("Env_HazardZone", 0.6f), ("Env_Wreck", 0.5f), ("Env_DebrisChunk", 2.5f),
                    ("Env_Crystal", 1.2f), ("Env_DriftMine", 0.8f), ("Env_BonusPod", 0.25f),
                };
                props.arraySize = want.Length;
                for (int i = 0; i < want.Length; i++)
                {
                    var e = props.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("prefab").objectReferenceValue = LoadAny(PrefabDir + want[i].name + ".prefab");
                    e.FindPropertyRelative("weight").floatValue = want[i].weight;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AddPrewarm(pool, new (string, int)[]
            {
                ("Env_Wreck", 6), ("Env_DebrisChunk", 90), ("Env_Crystal", 40),
                ("Env_DriftMine", 40), ("Env_BonusPod", 6),
            });

            // 3. A touch more parallax life on the nebula backdrop.
            if (starfield != null)
                new SerializedObject(starfield).Do(so =>
                {
                    so.FindProperty("_backdropParallax").floatValue = 0.022f;
                    so.FindProperty("_backdropDensity").floatValue = 1f; // 1 = tile at the sprite's native size (with PPU 32 ≈ 2 screen-widths)
                });

            // 4. Event banner on the HUD canvas.
            BuildEventBanner(ed);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildEventBanner(EventDirector ed)
        {
            var hud = GameObject.Find("HudCanvas");
            if (hud == null) { Debug.LogWarning("[M18] HudCanvas not found — skipping EventBanner"); return; }

            var existing = hud.transform.Find("EventBanner");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject("EventBanner", typeof(RectTransform));
            go.transform.SetParent(hud.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -120f);
            rt.sizeDelta = new Vector2(900f, 60f);

            var group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<UnityEngine.UI.Text>();
            label.transform.SetParent(go.transform, false);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 30;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 0.93f, 0.7f, 1f);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            var sh = label.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.8f);
            sh.effectDistance = new Vector2(2f, -2f);

            var banner = go.AddComponent<SpaceSurvivors.UI.EventBanner>();
            new SerializedObject(banner).Do(so =>
            {
                so.FindProperty("_director").objectReferenceValue = ed;
                so.FindProperty("_label").objectReferenceValue = label;
                so.FindProperty("_group").objectReferenceValue = group;
            });
        }

        // ---------------------------------------------------------------- helpers

        private static HealthData MakeHealth(string name, float hp)
        {
            var d = LoadOrCreate<HealthData>(HealthDir + name + ".asset");
            d.maxHealth = hp;
            d.invulnerabilityAfterHit = 0f;
            EditorUtility.SetDirty(d);
            return d;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<T>();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(a, path);
            }
            return a;
        }

        private static Sprite Spr(string sub)
            => AssetDatabase.LoadAssetAtPath<Sprite>(sub.StartsWith("Assets/") ? sub : Art + sub);

        private static Sprite P(string name)
        {
            string path = name is "WeaponOrb" or "WeaponOrbGlow"
                ? Art + "Sprites/Generated/" + name + ".png"
                : Art + "Particles/PNG (Transparent)/" + name + ".png";
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning($"[M18] missing sprite: {path}");
            return s;
        }

        private static Material Mat() => AssetDatabase.LoadAssetAtPath<Material>(GlowMat);

        private static GameObject LoadAny(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

        private static void Save(GameObject go, string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            PrefabUtility.SaveAsPrefabAsset(go, dir + go.name + ".prefab");
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void AddPrewarm(SpaceSurvivors.Core.PoolManager pool, (string name, int count)[] entries)
        {
            if (pool == null) return;
            var so = new SerializedObject(pool);
            var list = so.FindProperty("_prewarm");
            foreach (var (name, count) in entries)
            {
                var prefab = LoadAny(PrefabDir + name + ".prefab");
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

        private static void Do(this SerializedObject so, Action<SerializedObject> edit)
        {
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
