using System.Linq;
using SpaceSurvivors.Data;
using SpaceSurvivors.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// G5 — replaces the Mine Layer with Arc Coil, a chain-lightning weapon. The Mine assets
    /// and scripts are deleted by hand first; this creates the three assets that take their
    /// place and drops Arc Coil's unlock into the level-up catalogue where Mine Layer's was.
    ///
    /// <para>Idempotent: re-running overwrites the three assets in place and re-points the
    /// catalogue. Run <c>SpaceSurvivors/Build/G5 Arc Coil weapon</c>, then the M17 weapon-VFX
    /// pass is not needed — Arc Coil has no projectile, its bolt is drawn by
    /// <c>ChainArcView</c> at runtime.</para>
    /// </summary>
    internal static class ChainWeaponBuilder
    {
        private const string WeaponDir = "Assets/_Project/ScriptableObjects/Weapons";
        private const string UpgradeDir = "Assets/_Project/ScriptableObjects/Upgrades";
        private const string GameScene = "Assets/_Project/Scenes/Game.unity";
        private const string MultiShotPath = UpgradeDir + "/MultiShot.asset";

        [MenuItem("SpaceSurvivors/Build/G5 Arc Coil weapon")]
        private static void Build()
        {
            var evolution = Weapon($"{WeaponDir}/ChainStorm.asset", w =>
            {
                w.displayName = "Chain Storm";
                w.kind = WeaponKind.Chain;
                w.cooldown = 0.78f;
                w.damage = 21f;
                w.projectilesPerShot = 5;    // base targets; MultiShot adds more
                w.chainRange = 4.6f;
                w.chainFalloff = 0.86f;
                w.aimRange = 9f;
                w.chainTint = new Color(0.78f, 0.62f, 1f, 1f);   // violet — reads as the "evolved" tier
            });

            var arcCoil = Weapon($"{WeaponDir}/ArcCoil.asset", w =>
            {
                w.displayName = "Arc Coil";
                w.kind = WeaponKind.Chain;
                w.cooldown = 1.15f;
                w.damage = 14f;
                w.projectilesPerShot = 3;
                w.chainRange = 3.5f;
                w.chainFalloff = 0.72f;
                w.aimRange = 8f;
                w.chainTint = new Color(0.55f, 0.85f, 1f, 1f);   // ice blue
                w.evolvesInto = evolution;
                w.evolutionCatalyst = Load<UpgradeData>(MultiShotPath);   // more jumps → the fantasy
            });

            var unlock = Upgrade($"{UpgradeDir}/GetArcCoil.asset", u =>
            {
                u.title = "Arc Coil";
                u.description = "A bolt that leaps between nearby enemies";
                u.special = UpgradeData.SpecialEffect.GrantWeapon;
                u.weaponToGrant = arcCoil;
                u.weight = 0.6f;
                u.maxStacks = 1;
            });

            SwapInCatalogue(unlock);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ChainWeaponBuilder] Arc Coil + Chain Storm + GetArcCoil built; catalogue re-pointed.");
        }

        private static WeaponData Weapon(string path, System.Action<WeaponData> configure)
        {
            var asset = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            bool isNew = asset == null;
            if (isNew) asset = ScriptableObject.CreateInstance<WeaponData>();

            configure(asset);

            if (isNew) AssetDatabase.CreateAsset(asset, path);
            else EditorUtility.SetDirty(asset);
            return asset;
        }

        private static UpgradeData Upgrade(string path, System.Action<UpgradeData> configure)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
            bool isNew = asset == null;
            if (isNew) asset = ScriptableObject.CreateInstance<UpgradeData>();

            configure(asset);

            if (isNew) AssetDatabase.CreateAsset(asset, path);
            else EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T Load<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) Debug.LogWarning($"[ChainWeaponBuilder] missing {path}");
            return a;
        }

        /// <summary>
        /// Put <paramref name="unlock"/> into every <see cref="UpgradeService"/> catalogue,
        /// dropping any null slot left behind by the deleted GetMineLayer asset. The list is a
        /// serialized field on a scene component, so this edits the scene and saves it.
        /// </summary>
        private static void SwapInCatalogue(UpgradeData unlock)
        {
            var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

            int touched = 0;
            foreach (var service in Object.FindObjectsByType<UpgradeService>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(service);
                var list = so.FindProperty("_catalogue");

                // drop nulls (the vanished GetMineLayer)
                for (int i = list.arraySize - 1; i >= 0; i--)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        list.DeleteArrayElementAtIndex(i);

                bool has = Enumerable.Range(0, list.arraySize)
                    .Any(i => list.GetArrayElementAtIndex(i).objectReferenceValue == unlock);
                if (!has)
                {
                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = unlock;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                touched++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ChainWeaponBuilder] catalogue updated on {touched} UpgradeService(s).");
        }
    }
}
