using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSurvivors.Combat;
using SpaceSurvivors.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// M17 — weapon visual pass. Turns the flat Kenney-sprite projectiles into glowing
    /// energy: a global Bloom volume, an additive material, restyled base projectiles
    /// (core + soft glow + trail), and a distinct <c>Evo_*</c> prefab for every evolved
    /// weapon so an evolution actually looks different. Editor-only content generation
    /// (the RunCommand assembly can't build prefabs / touch render settings). Idempotent —
    /// safe to re-run. Menu: SpaceSurvivors/Build/M17 Weapon VFX.
    /// </summary>
    internal static class WeaponVfxBuilder
    {
        private const string Particles = "Assets/_Project/Art/Particles/PNG (Transparent)/";
        private const string MatPath = "Assets/_Project/Art/Particles/WeaponGlow.mat";
        private const string PrefabDir = "Assets/_Project/Prefabs/Projectiles/";
        private const string WeaponDir = "Assets/_Project/ScriptableObjects/Weapons/";
        private const string ProfileDir = "Assets/Settings/PostProcess/";
        private const string ProfilePath = ProfileDir + "GameVolume.asset";
        private const string GameScene = "Assets/_Project/Scenes/Game.unity";
        private const float ParticlePpu = 512f;

        private const string GenDir = "Assets/_Project/Art/Sprites/Generated/";

        [MenuItem("SpaceSurvivors/Build/M17 Weapon VFX")]
        public static void Build()
        {
            FixParticleImporters();
            BakeOrb();
            var mat = CreateAdditiveMaterial();
            CreateVolumeProfile();

            BuildMuzzleFlash(mat);
            StyleBaseProjectiles(mat);
            StyleImpacts(mat);
            BuildEvolutionPrefabs(mat);
            StyleAuras();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WireGameScene();

            Debug.Log("[WeaponVfxBuilder] M17 weapon VFX built.");
        }

        // ---------------------------------------------------------------- assets

        private static void FixParticleImporters()
        {
            foreach (var path in Directory.GetFiles(Particles, "*.png"))
            {
                var p = path.Replace('\\', '/');
                if (AssetImporter.GetAtPath(p) is not TextureImporter imp) continue;
                bool dirty = false;
                if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; dirty = true; }
                if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (!Mathf.Approximately(imp.spritePixelsPerUnit, ParticlePpu)) { imp.spritePixelsPerUnit = ParticlePpu; dirty = true; }
                if (imp.mipmapEnabled) { imp.mipmapEnabled = false; dirty = true; }
                if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; dirty = true; }
                if (dirty) imp.SaveAndReimport();
            }
            AssetDatabase.Refresh();
        }

        private static Sprite P(string name)
        {
            // "Orb" / "OrbGlow" come from our own baker; everything else from the Kenney pack.
            string path = name is "Orb" or "OrbGlow"
                ? GenDir + "Weapon" + name + ".png"
                : Particles + name + ".png";
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning($"[WeaponVfxBuilder] missing sprite '{name}' ({path})");
            return s;
        }

        /// <summary>
        /// Kenney's circles are all bubbles / rings — none is a clean solid orb. Bake two:
        /// a tight bright core (<c>WeaponOrb</c>) and a wide soft halo (<c>WeaponOrbGlow</c>),
        /// both plain radial gradients so an additive tint reads as a glowing ball.
        /// </summary>
        private static void BakeOrb()
        {
            Directory.CreateDirectory(GenDir);
            // WeaponOrb: a solid disc (full alpha to 55% radius, soft edge to 100%) so a tint
            // reads as a real coloured ball. WeaponOrbGlow: a wide gentle falloff halo.
            BakeRadial("WeaponOrb.png", 128, 0.55f, 1f);
            BakeRadial("WeaponOrbGlow.png", 128, 0f, 2f);
            BakeAuraField("AuraField.png", 256);
        }

        /// <summary>The Aura-weapon zone sprite: a faint inner wash (so the whole radius reads
        /// as "you are standing in it") plus a bright soft rim that draws the boundary. The old
        /// ring was a thin near-invisible outline. 1 sprite = 1 world unit.</summary>
        private static void BakeAuraField(string file, int size)
        {
            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f, dy = y - r + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                float a;
                if (d >= 1f)          a = 0f;
                else if (d < 0.80f)   a = 0.20f + 0.14f * (d / 0.80f);                    // inner wash, denser toward the edge
                else if (d < 0.93f)   a = Mathf.Lerp(0.34f, 1f, (d - 0.80f) / 0.13f);     // rim ramps up
                else                  a = Mathf.Lerp(1f, 0f, (d - 0.93f) / 0.07f);        // soft outer feather
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            WriteSprite(GenDir + file, size, px);
        }

        private static void WriteSprite(string path, int size, Color[] px)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter imp)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = size;   // 1 sprite = 1 world unit
                imp.filterMode = FilterMode.Bilinear;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
        }

        private static void BakeRadial(string file, int size, float solidFrac, float falloffPow)
        {
            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f, dy = y - r + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                float a;
                if (d >= 1f) a = 0f;
                else if (d <= solidFrac) a = 1f;
                else a = Mathf.Pow(1f - (d - solidFrac) / (1f - solidFrac), falloffPow);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            string path = GenDir + file;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter imp)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = size;   // 1 sprite = 1 world unit
                imp.filterMode = FilterMode.Bilinear;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
        }

        private static Material CreateAdditiveMaterial()
        {
            // Plain alpha-blended sprite shader: it faithfully keeps the SpriteRenderer /
            // TrailRenderer colour (a custom additive pass washed the coloured cores to white).
            // The "glow" is the global Bloom volume catching the bright sprite centres.
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("[WeaponVfxBuilder] Sprites/Default shader not found — aborting.");
                return null;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                mat = new Material(shader) { name = "WeaponGlow" };
                AssetDatabase.CreateAsset(mat, MatPath);
            }
            else
            {
                mat.shader = shader;
                if (mat.name != "WeaponGlow") mat.name = "WeaponGlow";
            }
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssetIfDirty(mat);
            return mat;
        }

        private static void CreateVolumeProfile()
        {
            Directory.CreateDirectory(ProfileDir);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            Bloom bloom = profile.TryGet(out Bloom b) ? b : profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.overrideState = true; bloom.threshold.value = 0.55f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.7f;
            bloom.scatter.overrideState = true; bloom.scatter.value = 0.5f;
            bloom.tint.overrideState = true; bloom.tint.value = Color.white;
            if (AssetDatabase.LoadAllAssetsAtPath(ProfilePath).All(a => a != bloom))
                AssetDatabase.AddObjectToAsset(bloom, profile);

            // No tonemapping — ACES/Neutral desaturate the bright saturated projectile cores
            // toward white, which is exactly what we're trying to avoid.
            if (profile.TryGet(out Tonemapping oldTone))
            {
                profile.Remove<Tonemapping>();
                Object.DestroyImmediate(oldTone, true);
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- styling

        /// <summary>One projectile's new look.</summary>
        private struct Look
        {
            public string core;        // core sprite (bright, small)
            public Color coreColor;
            public string glow;        // soft halo behind the core
            public Color glowColor;
            public float glowScale;    // local scale of the glow child
            public float rootScale;    // new root scale (collider is compensated)
            public bool trail;
            public Color trailColor;
            public float trailTime;
            public float pulse;        // >0 = breathe the glow child
            public float spin;         // deg/s spin on the root (orbs only)
        }

        private static void StyleBaseProjectiles(Material mat)
        {
            // No trail on the Laser or Scatter pellets — with the Laser now a rapid stream of
            // bolts, overlapping trails smeared into one solid line and you couldn't see the
            // individual shots. Trails stay on the projectiles that read as single streaks
            // (Rail Spike etc.), which keeps the "sniper" weapons visually distinct.
            Style("Laser", mat, new Look
            {
                core = "trace_06", coreColor = new Color(0.5f, 0.95f, 1f),
                rootScale = 0.5f, trail = false,
            });
            Style("Missile", mat, new Look
            {
                core = "Orb", coreColor = new Color(1f, 0.55f, 0.2f),
                rootScale = 0.42f, trail = true, trailColor = new Color(1f, 0.5f, 0.18f), trailTime = 0.16f,
            });
            Style("PlasmaOrb", mat, new Look
            {
                core = "Orb", coreColor = new Color(0.66f, 0.32f, 1f),
                rootScale = 0.5f, trail = true, trailColor = new Color(0.6f, 0.35f, 1f), trailTime = 0.12f,
                pulse = 0.14f,
            });
            Style("ScatterPellet", mat, new Look
            {
                core = "Orb", coreColor = new Color(1f, 0.66f, 0.24f),
                rootScale = 0.32f, trail = false,
            });
            Style("RailShard", mat, new Look
            {
                core = "trace_04", coreColor = new Color(0.78f, 0.92f, 1f),
                rootScale = 0.85f, trail = true, trailColor = new Color(0.7f, 0.88f, 1f), trailTime = 0.13f,
            });
            Style("OrbitOrb", mat, new Look
            {
                core = "Orb", coreColor = new Color(0.2f, 1f, 0.8f),
                rootScale = 0.52f, pulse = 0.16f, spin = 90f,
            });
            Style("Mine", mat, new Look
            {
                core = "Orb", coreColor = new Color(1f, 0.28f, 0.24f),
                rootScale = 0.5f, pulse = 0.22f,
            });
        }

        private static void Style(string prefabName, Material mat, Look look)
        {
            var path = PrefabDir + prefabName + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) { Debug.LogWarning($"[WeaponVfxBuilder] no prefab {path}"); return; }

            try
            {
                ApplyLook(root, mat, look);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyLook(GameObject root, Material mat, Look look)
        {
            // Root scale drives the visual size; keep the collider (gameplay hitbox) constant.
            float oldScale = root.transform.localScale.x;
            float newScale = look.rootScale > 0f ? look.rootScale : oldScale;
            if (!Mathf.Approximately(oldScale, newScale) && newScale > 0f)
            {
                float k = oldScale / newScale;
                root.transform.localScale = Vector3.one * newScale;
                foreach (var c in root.GetComponentsInChildren<CircleCollider2D>(true)) c.radius *= k;
                foreach (var c in root.GetComponentsInChildren<BoxCollider2D>(true)) c.size *= k;
            }

            // Core = the root SpriteRenderer. A small, saturated shape; the Bloom volume is
            // what makes it "glow", so we deliberately do NOT stack a halo sprite on top
            // (that just blows out to white).
            var core = root.GetComponent<SpriteRenderer>();
            if (core == null) core = root.AddComponent<SpriteRenderer>();
            core.sprite = P(look.core);
            core.color = look.coreColor;
            core.sharedMaterial = mat;
            core.sortingOrder = 6;

            // Optional faint halo child — only when a glow sprite is named, kept dim.
            var existingGlow = root.transform.Find("Glow");
            if (string.IsNullOrEmpty(look.glow))
            {
                if (existingGlow != null) Object.DestroyImmediate(existingGlow.gameObject, true);
            }
            else
            {
                var glow = existingGlow != null ? existingGlow.gameObject : FindOrAddChild(root, "Glow");
                var gsr = glow.GetComponent<SpriteRenderer>();
                if (gsr == null) gsr = glow.AddComponent<SpriteRenderer>();
                gsr.sprite = P(look.glow);
                gsr.color = look.glowColor;
                gsr.sharedMaterial = mat;
                gsr.sortingOrder = 5;
                glow.transform.localScale = Vector3.one * Mathf.Max(0.01f, look.glowScale);
            }

            // Spin / pulse ride the root (orbs don't rotate from gameplay; bolts are set by
            // Projectile.Launch each spawn, so a pulse-only VfxSpinPulse is safe there too).
            StripComponent<VfxSpinPulse>(root);
            if (look.spin != 0f || look.pulse > 0f)
            {
                var vs = root.AddComponent<VfxSpinPulse>();
                if (look.spin != 0f) SetPrivate(vs, "_spinDegreesPerSecond", look.spin);
                if (look.pulse > 0f)
                {
                    SetPrivate(vs, "_pulseAmount", look.pulse);
                    SetPrivate(vs, "_pulseFrequency", 3f);
                }
            }

            // Trail.
            var trail = root.GetComponent<TrailRenderer>();
            if (look.trail)
            {
                if (trail == null) trail = root.AddComponent<TrailRenderer>();
                // Trail width tracks the projectile size (root scale) so it never dwarfs it.
                ConfigureTrail(trail, mat, look.trailColor, look.trailTime, newScale * 0.5f);
                if (root.GetComponent<TrailReset>() == null) root.AddComponent<TrailReset>();
            }
            else if (trail != null)
            {
                // TrailReset has [RequireComponent(TrailRenderer)] — drop it first or Unity
                // refuses to remove the renderer.
                if (root.GetComponent<TrailReset>() is { } tr2) Object.DestroyImmediate(tr2, true);
                Object.DestroyImmediate(trail, true);
            }
        }

        private static void ConfigureTrail(TrailRenderer tr, Material mat, Color tint, float time, float width)
        {
            tr.sharedMaterial = mat;
            tr.time = time;
            tr.minVertexDistance = 0.03f;
            tr.numCapVertices = 4;
            tr.numCornerVertices = 2;
            tr.alignment = LineAlignment.View;
            tr.textureMode = LineTextureMode.Stretch;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.sortingOrder = 4;

            tr.widthCurve = new AnimationCurve(
                new Keyframe(0f, Mathf.Max(0.03f, width)), new Keyframe(1f, 0f));

            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(tint, 0f), new GradientColorKey(tint, 1f) },
                new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = grad;
        }

        private static void StyleImpacts(Material mat)
        {
            RetintImpact("LaserImpact", mat, new Color(0.5f, 0.95f, 1f));
            RetintImpact("MissileImpact", mat, new Color(1f, 0.55f, 0.2f));
            RetintImpact("PlasmaBoom", mat, new Color(0.75f, 0.5f, 1f));
        }

        private static void RetintImpact(string prefabName, Material mat, Color tint)
        {
            var path = PrefabDir + prefabName + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) return;
            try
            {
                foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.sharedMaterial = mat;
                    var c = tint; c.a = sr.color.a;
                    sr.color = c;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildMuzzleFlash(Material mat)
        {
            var path = PrefabDir + "MuzzleFlash.prefab";
            var go = new GameObject("MuzzleFlash");
            try
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = P("muzzle_03");
                sr.color = new Color(0.7f, 0.85f, 1f, 0.5f);
                sr.sharedMaterial = mat;
                sr.sortingOrder = 7;
                // Kenney muzzle sprites point up; the shot direction is +X, so rotate -90.
                go.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                go.transform.localScale = Vector3.one * 0.3f;

                go.AddComponent<SpaceSurvivors.Core.PoolHandle>();
                var pulse = go.AddComponent<OneShotPulse>();
                SetPrivate(pulse, "_lifetime", 0.08f);
                SetPrivate(pulse, "_startScale", 0.28f);
                SetPrivate(pulse, "_endScale", 0.42f);

                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static void StyleAuras()
        {
            // The aura was too faint and too small to notice. Bigger radius + a much stronger
            // tint on the new AuraField sprite (faint fill + bright rim).
            SetAura("StaticField", new Color(0.45f, 0.9f, 1f, 0.5f), radius: 3.4f);
            SetAura("IonStorm",    new Color(0.82f, 0.55f, 1f, 0.6f), radius: 4.4f);
        }

        private static void SetAura(string weaponAsset, Color tint, float radius)
        {
            var wd = AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponDir + weaponAsset + ".asset");
            if (wd == null) return;
            var so = new SerializedObject(wd);
            so.FindProperty("auraTint").colorValue = tint;
            so.FindProperty("orbitRadius").floatValue = radius;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wd);
        }

        // ---------------------------------------------------------------- evolutions

        private static void BuildEvolutionPrefabs(Material mat)
        {
            // Evolutions read as "bigger + different colour + more motion" than their base.
            Evolve("Laser", "Evo_PrismBolt", "PrismLaser", mat, new Look
            {
                core = "trace_06", coreColor = new Color(0.9f, 1f, 1f),
                rootScale = 0.72f, trail = false, pulse = 0.1f,
            });
            Evolve("Missile", "Evo_ClusterMissile", "ClusterMissile", mat, new Look
            {
                core = "Orb", coreColor = new Color(1f, 0.38f, 0.12f),
                glow = "flame_03", glowColor = new Color(1f, 0.3f, 0.06f, 0.4f), glowScale = 2.0f,
                rootScale = 0.46f, trail = true, trailColor = new Color(1f, 0.35f, 0.1f), trailTime = 0.22f,
            });
            Evolve("PlasmaOrb", "Evo_NovaOrb", "NovaCore", mat, new Look
            {
                core = "magic_04", coreColor = new Color(1f, 0.9f, 0.72f),
                rootScale = 0.5f, trail = true, trailColor = new Color(0.85f, 0.75f, 1f), trailTime = 0.15f,
                pulse = 0.2f, spin = 150f,
            });
            Evolve("ScatterPellet", "Evo_BuckshotPellet", "BuckshotStorm", mat, new Look
            {
                core = "Orb", coreColor = new Color(1f, 0.62f, 0.22f),
                rootScale = 0.4f, trail = false,
            });
            Evolve("RailShard", "Evo_VoidSliver", "VoidLance", mat, new Look
            {
                core = "trace_04", coreColor = new Color(0.62f, 0.42f, 1f),
                rootScale = 0.95f, trail = true, trailColor = new Color(0.5f, 0.28f, 1f), trailTime = 0.3f,
            });
            Evolve("OrbitOrb", "Evo_EventHorizonOrb", "EventHorizon", mat, new Look
            {
                core = "Orb", coreColor = new Color(0.55f, 0.15f, 1f),
                glow = "twirl_02", glowColor = new Color(0.55f, 0.25f, 1f, 0.5f), glowScale = 2.4f,
                rootScale = 0.58f, pulse = 0.12f, spin = -200f,
            });
            Evolve("Mine", "Evo_DeepMine", "DeepMine", mat, new Look
            {
                core = "Orb", coreColor = new Color(1f, 0.2f, 0.36f),
                rootScale = 0.56f, pulse = 0.28f,
            });
        }

        private static void Evolve(string baseName, string evoName, string weaponAsset, Material mat, Look look)
        {
            var basePath = PrefabDir + baseName + ".prefab";
            var evoPath = PrefabDir + evoName + ".prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(evoPath) == null)
            {
                if (!AssetDatabase.CopyAsset(basePath, evoPath))
                {
                    Debug.LogWarning($"[WeaponVfxBuilder] could not copy {basePath} -> {evoPath}");
                    return;
                }
            }
            AssetDatabase.ImportAsset(evoPath);

            var root = PrefabUtility.LoadPrefabContents(evoPath);
            try
            {
                root.name = evoName;
                ApplyLook(root, mat, look);
                PrefabUtility.SaveAsPrefabAsset(root, evoPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            var evoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(evoPath);
            var wd = AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponDir + weaponAsset + ".asset");
            if (wd != null && evoPrefab != null)
            {
                var so = new SerializedObject(wd);
                so.FindProperty("projectilePrefab").objectReferenceValue = evoPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------------------------------------------------------------- scene

        private static void WireGameScene()
        {
            var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

            // 1. Camera post-processing.
            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                EditorUtility.SetDirty(cam);
                EditorUtility.SetDirty(data);
            }

            // 2. Global bloom volume.
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            var existing = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)
                .FirstOrDefault(v => v.gameObject.name == "Global Volume");
            if (existing == null)
            {
                var go = new GameObject("Global Volume");
                existing = go.AddComponent<Volume>();
            }
            existing.isGlobal = true;
            existing.priority = 0f;
            existing.sharedProfile = profile;
            EditorUtility.SetDirty(existing);

            // 3. Muzzle flash + the aura zone sprite on the player's WeaponController.
            //    (Aura stays alpha-blended, not additive — additive just makes a white blob.)
            var wc = Object.FindFirstObjectByType<WeaponController>();
            var muzzle = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "MuzzleFlash.prefab");
            var auraField = AssetDatabase.LoadAssetAtPath<Sprite>(GenDir + "AuraField.png");
            if (wc != null)
            {
                var so = new SerializedObject(wc);
                if (muzzle != null) so.FindProperty("_muzzleFlashPrefab").objectReferenceValue = muzzle;
                if (auraField != null) so.FindProperty("_auraRingSprite").objectReferenceValue = auraField;
                so.FindProperty("_auraRingMaterial").objectReferenceValue = null; // default sprite shader
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ---------------------------------------------------------------- helpers

        private static GameObject FindOrAddChild(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void StripComponent<T>(GameObject go) where T : Component
        {
            foreach (var c in go.GetComponents<T>()) Object.DestroyImmediate(c, true);
        }

        private static void SetPrivate(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null) { prop.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }
}
