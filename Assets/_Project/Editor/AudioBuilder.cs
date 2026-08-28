using System.Collections.Generic;
using System.IO;
using SpaceSurvivors.Data;
using SpaceSurvivors.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// M10 wave 3 — creates the <see cref="SfxBank"/> asset from the Kenney SFX packs,
    /// tunes their import settings, and drops an <see cref="AudioDirector"/> (+ a
    /// <see cref="ButtonSfxInstaller"/>) into both scenes. Idempotent. Music is left for later.
    /// </summary>
    internal static class AudioBuilder
    {
        private const string Sfx = "Assets/_Project/Audio/SFX/";
        private const string BankPath = "Assets/_Project/ScriptableObjects/Config/SfxBank.asset";

        [MenuItem("SpaceSurvivors/Build/Audio (SFX bank + directors)")]
        private static void Build()
        {
            var bank = BuildBank();
            TuneImports();
            AddDirector("Assets/_Project/Scenes/Game.unity", bank);
            AddDirector("Assets/_Project/Scenes/MainMenu.unity", bank);
            Debug.Log("[Audio] SFX bank built + AudioDirector added to both scenes.");
        }

        // ---------------------------------------------------------------- bank

        private static AudioClip C(string rel) => AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + rel);
        private static AudioClip[] Cs(params string[] rels)
        {
            var list = new List<AudioClip>();
            foreach (var r in rels) { var c = C(r); if (c != null) list.Add(c); }
            return list.ToArray();
        }

        private static SfxBank BuildBank()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SfxBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<SfxBank>();
                Directory.CreateDirectory(Path.GetDirectoryName(BankPath));
                AssetDatabase.CreateAsset(bank, BankPath);
            }

            var so = new SerializedObject(bank);
            var entries = so.FindProperty("_entries");
            entries.ClearArray();

            void Add(SfxId id, float vol, Vector2 pitch, float minInterval, params string[] clips)
            {
                int i = entries.arraySize;
                entries.InsertArrayElementAtIndex(i);
                var e = entries.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = (int)id;
                e.FindPropertyRelative("volume").floatValue = vol;
                e.FindPropertyRelative("pitch").vector2Value = pitch;
                e.FindPropertyRelative("minInterval").floatValue = minInterval;
                var arr = e.FindPropertyRelative("clips");
                var resolved = Cs(clips);
                arr.arraySize = resolved.Length;
                for (int k = 0; k < resolved.Length; k++)
                    arr.GetArrayElementAtIndex(k).objectReferenceValue = resolved[k];
            }

            Add(SfxId.PlayerShootLaser,   0.32f, new Vector2(0.94f, 1.06f), 0.04f,
                "Kenney_SciFi/laserSmall_000.ogg", "Kenney_SciFi/laserSmall_001.ogg", "Kenney_SciFi/laserSmall_002.ogg");
            Add(SfxId.PlayerShootMissile, 0.5f,  new Vector2(0.9f, 1.05f), 0.05f,
                "Kenney_SciFi/laserLarge_000.ogg", "Kenney_SciFi/laserLarge_002.ogg");
            Add(SfxId.EnemyDeath,         0.5f,  new Vector2(0.88f, 1.12f), 0.03f,
                "Kenney_SciFi/explosionCrunch_000.ogg", "Kenney_SciFi/explosionCrunch_001.ogg",
                "Kenney_SciFi/explosionCrunch_002.ogg", "Kenney_SciFi/explosionCrunch_003.ogg");
            Add(SfxId.BossDeath,          0.9f,  new Vector2(0.95f, 1f), 0f,
                "Kenney_SciFi/lowFrequency_explosion_000.ogg", "Kenney_SciFi/lowFrequency_explosion_001.ogg");
            Add(SfxId.PlayerHurt,         0.7f,  new Vector2(0.92f, 1.05f), 0.15f,
                "Kenney_SciFi/impactMetal_001.ogg", "Kenney_SciFi/impactMetal_003.ogg");
            Add(SfxId.ShieldAbsorb,       0.6f,  new Vector2(0.95f, 1.05f), 0.1f,
                "Kenney_SciFi/forceField_000.ogg", "Kenney_SciFi/forceField_002.ogg");
            Add(SfxId.PickupCollect,      0.28f, new Vector2(0.96f, 1.12f), 0.02f,
                "Kenney_Interface/pluck_001.ogg", "Kenney_Interface/pluck_002.ogg");
            Add(SfxId.LevelUp,            0.7f,  new Vector2(1f, 1f), 0f,
                "Kenney_Interface/maximize_006.ogg");
            Add(SfxId.BossWarning,        0.8f,  new Vector2(1f, 1f), 0f,
                "Kenney_Interface/glitch_002.ogg");
            Add(SfxId.UiClick,            0.5f,  new Vector2(1f, 1f), 0.03f,
                "Kenney_UI/click1.ogg");
            Add(SfxId.UiHover,            0.25f, new Vector2(1f, 1f), 0.04f,
                "Kenney_UI/rollover1.ogg");
            Add(SfxId.RunWon,             0.9f,  new Vector2(1f, 1f), 0f,
                "Kenney_Interface/maximize_009.ogg");
            Add(SfxId.RunLost,            0.9f,  new Vector2(1f, 1f), 0f,
                "Kenney_Interface/error_004.ogg");

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return bank;
        }

        // ---------------------------------------------------------------- imports

        private static void TuneImports()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio/SFX" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as AudioImporter;
                if (imp == null) continue;
                var s = imp.defaultSampleSettings;
                s.loadType = AudioClipLoadType.DecompressOnLoad;   // short one-shots
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
                if (imp.defaultSampleSettings.loadType != s.loadType
                    || !Mathf.Approximately(imp.defaultSampleSettings.quality, s.quality))
                {
                    imp.defaultSampleSettings = s;
                    imp.forceToMono = true;
                    imp.SaveAndReimport();
                }
            }
        }

        // ---------------------------------------------------------------- scene wiring

        private static void AddDirector(string scenePath, SfxBank bank)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            // re-load by path — the reference passed in can be stale right after CreateAsset
            var freshBank = AssetDatabase.LoadAssetAtPath<SfxBank>(BankPath) ?? bank;

            GameObject go = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "AudioDirector") go = root;
            if (go == null)
            {
                go = new GameObject("AudioDirector");
                EditorSceneManager.MoveGameObjectToScene(go, scene);
            }

            var dir = go.GetComponent<AudioDirector>() ?? go.AddComponent<AudioDirector>();
            var so = new SerializedObject(dir);
            so.FindProperty("_bank").objectReferenceValue = freshBank;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dir);

            var installer = go.GetComponent<ButtonSfxInstaller>() ?? go.AddComponent<ButtonSfxInstaller>();
            var iso = new SerializedObject(installer);
            iso.FindProperty("_audio").objectReferenceValue = dir;
            iso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
