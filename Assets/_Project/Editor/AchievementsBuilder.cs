using System.Collections.Generic;
using System.Linq;
using SpaceSurvivors.Data;
using SpaceSurvivors.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// Builds the M14c achievements feature: seeds the <c>AchievementCatalogue</c> + its
    /// achievement assets in <c>Resources/</c>, builds <c>Assets/_Project/Scenes/Achievements.unity</c>
    /// (a 2×4 tile grid over the catalogue), and adds an ACHIEVEMENTS button to the main menu.
    /// Editor tool because the RunCommand assembly can't reference UnityEngine.UI. Idempotent.
    /// </summary>
    internal static class AchievementsBuilder
    {
        private const string Ui = "Assets/_Project/Art/UI/PNG/";
        private const string Art = "Assets/_Project/Art/";
        private const string ResDir = "Assets/_Project/Resources/";
        private const string ScenePath = "Assets/_Project/Scenes/Achievements.unity";
        private const string CataloguePath = ResDir + "AchievementCatalogue.asset";

        private static Font Legacy => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static Sprite S(string sub) => AssetDatabase.LoadAssetAtPath<Sprite>(Ui + sub);
        /// <summary>Icon by path relative to <c>Assets/_Project/Art/</c>.</summary>
        private static Sprite Ico(string sub) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + sub);

        private static readonly Color Steel = new(0.80f, 0.84f, 0.90f);
        private static readonly Color Gold = new(1f, 0.85f, 0.35f);
        private static readonly Color Ink = new(0.72f, 0.82f, 0.95f);

        // ---------------------------------------------------------------- catalogue seed

        private readonly struct Seed
        {
            public readonly string Id, Title, Desc, Icon;
            public readonly AchievementMetric Metric;
            public readonly long Threshold;
            public Seed(string id, string title, string desc, string icon, AchievementMetric metric, long threshold)
            { Id = id; Title = title; Desc = desc; Icon = icon; Metric = metric; Threshold = threshold; }
        }

        private static readonly Seed[] Seeds =
        {
            new("first_blood", "First Blood", "Destroy your first enemy.",
                "Sprites/Base_Assets/Power-ups/star_bronze.png", AchievementMetric.LifetimeKills, 1),
            new("swarm_culler", "Swarm Culler", "Destroy 500 enemies in total.",
                "Sprites/Base_Assets/Power-ups/star_silver.png", AchievementMetric.LifetimeKills, 500),
            new("exterminator", "Exterminator", "Destroy 5,000 enemies in total.",
                "Sprites/Base_Assets/Power-ups/star_gold.png", AchievementMetric.LifetimeKills, 5000),
            new("survivor", "Survivor", "Survive for 5 minutes in a single run.",
                "UI/PNG/Main_UI/Clock_Icon.png", AchievementMetric.BestSurvivalSeconds, 300),
            new("unbreakable", "Unbreakable", "Survive for 10 minutes in a single run.",
                "Sprites/Base_Assets/Power-ups/shield_gold.png", AchievementMetric.BestSurvivalSeconds, 600),
            new("giant_slayer", "Giant Slayer", "Defeat a boss.",
                "Sprites/Base_Assets/Power-ups/bolt_gold.png", AchievementMetric.BossKills, 1),
            new("scrap_baron", "Scrap Baron", "Earn 5,000 scrap in total.",
                "Sprites/Generated/ScrapChip.png", AchievementMetric.LifetimeScrap, 5000),
            new("full_hangar", "Full Hangar", "Own every ship in the hangar.",
                "Sprites/Base_Assets/playerShip3_orange.png", AchievementMetric.ShipsOwned, 0),
        };

        [MenuItem("SpaceSurvivors/Build/M14c Seed achievement catalogue")]
        private static AchievementCatalogue SeedCatalogue()
        {
            var cat = AssetDatabase.LoadAssetAtPath<AchievementCatalogue>(CataloguePath);
            if (cat == null)
            {
                cat = ScriptableObject.CreateInstance<AchievementCatalogue>();
                AssetDatabase.CreateAsset(cat, CataloguePath);
            }
            cat.achievements.Clear();

            foreach (var s in Seeds)
            {
                string path = ResDir + "Ach_" + s.Id + ".asset";
                var a = AssetDatabase.LoadAssetAtPath<AchievementData>(path);
                if (a == null)
                {
                    a = ScriptableObject.CreateInstance<AchievementData>();
                    AssetDatabase.CreateAsset(a, path);
                }
                a.id = s.Id;
                a.title = s.Title;
                a.description = s.Desc;
                a.metric = s.Metric;
                a.threshold = s.Threshold;
                a.icon = Ico(s.Icon);
                if (a.icon == null) Debug.LogWarning($"[AchievementsBuilder] icon not found for '{s.Id}': {s.Icon}");
                EditorUtility.SetDirty(a);
                cat.achievements.Add(a);
            }

            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AchievementsBuilder] seeded {cat.achievements.Count} achievements → {CataloguePath}");
            return cat;
        }

        // ---------------------------------------------------------------- scene

        [MenuItem("SpaceSurvivors/Build/M14c Achievements scene")]
        private static void Build()
        {
            SetBorder("Rating/Window.png", 60, 120, 60, 120);
            SetBorder("Rating/Table_01.png", 30, 30, 30, 30);
            SetBorder("Rating/Table_02.png", 30, 30, 30, 30);
            SetBorder("Shop/Prise_BTN_Table.png", 40, 40, 40, 40);

            var cat = AssetDatabase.LoadAssetAtPath<AchievementCatalogue>(CataloguePath) ?? SeedCatalogue();
            var list = cat.achievements.Where(a => a != null).ToList();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
            cam.orthographic = true;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("AchievementsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = Img("Background", canvasGo.transform, S("Main_Menu/BG.png"), Color.white, raycast: true);
            Stretch(bg.rectTransform);
            if (bg.sprite == null) bg.color = new Color(0.05f, 0.06f, 0.10f);

            var screen = canvasGo.AddComponent<AchievementsScreen>();

            var panel = Img("Panel", canvasGo.transform, S("Rating/Window.png"), Color.white);
            panel.type = Image.Type.Sliced;
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(1180, 1040), Vector2.zero);

            // The Rating/Header.png sprite has "RATING" baked in — use a plain label on the
            // panel's dark title bar instead (matches the map-select screen).
            var headerText = Label("HeaderText", panel.transform, "ACHIEVEMENTS", 38, new Color(0.96f, 0.98f, 1f));
            headerText.fontStyle = FontStyle.Bold;
            Place(headerText, new Vector2(0.5f, 1f), new Vector2(700, 66), new Vector2(0, -50));

            var summary = Label("Summary", panel.transform, "0 / 0 UNLOCKED", 26, Gold);
            summary.fontStyle = FontStyle.Bold;
            Place(summary, new Vector2(0.5f, 1f), new Vector2(400, 38), new Vector2(0, -104));

            // ---- 2 × 4 tile grid ----
            var tiles = new List<AchievementsScreen.Tile>();
            const int cols = 2;
            const float tileW = 540f, tileH = 158f, gapX = 20f, gapY = 22f;
            float x0 = -(tileW + gapX) / 2f;
            float y0 = 208f;

            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                int col = i % cols, rowIx = i / cols;
                var pos = new Vector2(x0 + col * (tileW + gapX), y0 - rowIx * (tileH + gapY));

                var bgImg = Img($"Tile_{a.id}", panel.transform, S("Rating/Table_01.png"), Color.white);
                bgImg.type = Image.Type.Sliced;
                Place(bgImg, new Vector2(0.5f, 0.5f), new Vector2(tileW, tileH), pos);

                var icon = Img("Icon", bgImg.transform, a.icon, Color.white);
                icon.preserveAspect = true;
                Place(icon, new Vector2(0f, 0.5f), new Vector2(88, 88), new Vector2(66, 4));

                var title = Label("Title", bgImg.transform, a.title, 25, new Color(1f, 0.95f, 0.8f));
                title.alignment = TextAnchor.LowerLeft;
                title.fontStyle = FontStyle.Bold;
                Place(title, new Vector2(0f, 0.5f), new Vector2(360, 36), new Vector2(340, 40));
                Shadow(title);

                var desc = Label("Desc", bgImg.transform, a.description, 16, Ink);
                desc.alignment = TextAnchor.UpperLeft;
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(desc, new Vector2(0f, 0.5f), new Vector2(360, 44), new Vector2(340, 2));
                Shadow(desc);

                var progress = Label("Progress", bgImg.transform, "", 19, Steel);
                progress.alignment = TextAnchor.LowerLeft;
                progress.fontStyle = FontStyle.Bold;
                Place(progress, new Vector2(0f, 0.5f), new Vector2(360, 28), new Vector2(340, -50));
                Shadow(progress);

                tiles.Add(new AchievementsScreen.Tile
                {
                    achievementId = a.id,
                    icon = icon,
                    title = title,
                    description = desc,
                    progress = progress,
                    unlockedBadge = null,
                    background = bgImg,
                });
            }

            var back = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            back.transform.SetParent(panel.transform, false);
            var backImg = back.GetComponent<Image>();
            backImg.sprite = S("Shop/Prise_BTN_Table.png");
            backImg.type = Image.Type.Sliced;
            Place(back.transform, new Vector2(0.5f, 0f), new Vector2(300, 82), new Vector2(0, 38));
            var backLabel = Label("Label", back.transform, "BACK", 28, new Color(0.95f, 0.98f, 1f));
            Stretch(backLabel.rectTransform);

            var so = new SerializedObject(screen);
            so.FindProperty("_summaryLabel").objectReferenceValue = summary;
            so.FindProperty("_backButton").objectReferenceValue = back.GetComponent<Button>();
            so.FindProperty("_menuSceneName").stringValue = "MainMenu";
            var tilesProp = so.FindProperty("_tiles");
            tilesProp.arraySize = tiles.Count;
            for (int i = 0; i < tiles.Count; i++)
            {
                var e = tilesProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("achievementId").stringValue = tiles[i].achievementId;
                e.FindPropertyRelative("icon").objectReferenceValue = tiles[i].icon;
                e.FindPropertyRelative("title").objectReferenceValue = tiles[i].title;
                e.FindPropertyRelative("description").objectReferenceValue = tiles[i].description;
                e.FindPropertyRelative("progress").objectReferenceValue = tiles[i].progress;
                e.FindPropertyRelative("unlockedBadge").objectReferenceValue = tiles[i].unlockedBadge;
                e.FindPropertyRelative("background").objectReferenceValue = tiles[i].background;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScene();
            Debug.Log($"[AchievementsBuilder] built {ScenePath} with {tiles.Count} tiles.");
        }

        [MenuItem("SpaceSurvivors/Build/M14c Main-Menu Achievements button")]
        private static void BuildMenuButton()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

            var all = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            var anchorBtn = all.FirstOrDefault(b => b.name == "HangarButton")
                         ?? all.FirstOrDefault(b => b.name == "ShopButton")
                         ?? all.FirstOrDefault(b => b.name == "SettingsButton");
            var anchor = anchorBtn != null ? (RectTransform)anchorBtn.transform : null;
            var parent = anchor != null ? anchor.parent : Object.FindAnyObjectByType<Canvas>().transform;

            var existing = parent.Find("AchievementsButton");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject("AchievementsButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LoadSceneButton));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = S("Shop/Prise_BTN_Table.png");
            img.type = Image.Type.Sliced;
            var rt = (RectTransform)go.transform;
            if (anchor != null)
            {
                rt.anchorMin = anchor.anchorMin; rt.anchorMax = anchor.anchorMax; rt.pivot = anchor.pivot;
                rt.sizeDelta = anchor.sizeDelta;
                rt.anchoredPosition = anchor.anchoredPosition + new Vector2(0, -(anchor.sizeDelta.y + 14));
            }
            else
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
                rt.sizeDelta = new Vector2(230, 74);
                rt.anchoredPosition = new Vector2(-40, -304);
            }

            var label = Label("Label", go.transform, "ACHIEVEMENTS", 19, new Color(0.95f, 0.98f, 1f));
            Stretch(label.rectTransform);

            var lso = new SerializedObject(go.GetComponent<LoadSceneButton>());
            lso.FindProperty("_sceneName").stringValue = "Achievements";
            lso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[AchievementsBuilder] added ACHIEVEMENTS button to MainMenu.");
        }

        private static void RegisterScene()
        {
            var listS = EditorBuildSettings.scenes.ToList();
            if (listS.Any(s => s.path == ScenePath)) return;
            listS.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = listS.ToArray();
        }

        // ---------------------------------------------------------------- primitives

        private static Image Img(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var im = go.GetComponent<Image>();
            im.sprite = sprite;
            im.color = color;
            im.raycastTarget = raycast;
            return im;
        }

        private static Text Label(string name, Transform parent, string text, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Legacy;
            t.fontSize = size;
            t.color = color;
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        /// <summary>Drop shadow so tile text stays legible over the panel art's bright swoosh.</summary>
        private static void Shadow(Text t)
        {
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.75f);
            s.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private static void Place(Component c, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private static void SetBorder(string sub, int l, int b, int r, int t)
        {
            var importer = AssetImporter.GetAtPath(Ui + sub) as TextureImporter;
            if (importer == null) return;
            var want = new Vector4(l, b, r, t);
            if (importer.spriteBorder == want) return;
            importer.spriteBorder = want;
            importer.SaveAndReimport();
        }
    }
}
