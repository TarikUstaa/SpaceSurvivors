using System.Collections.Generic;
using System.Linq;
using SpaceSurvivors.Data;
using SpaceSurvivors.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// Builds <c>Assets/_Project/Scenes/Shop.unity</c> (M14a — permanent upgrade shop) from
    /// the CraftPix kit + the <c>MetaUpgradeCatalogue</c>. Editor tool because the Unity-MCP
    /// RunCommand assembly can't reference UnityEngine.UI. Idempotent — recreates the scene
    /// from scratch each run and re-registers it in Build Settings.
    /// </summary>
    internal static class ShopBuilder
    {
        private const string Ui = "Assets/_Project/Art/UI/PNG/";
        private const string ScenePath = "Assets/_Project/Scenes/Shop.unity";
        private static Font Legacy => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static Sprite S(string sub) => AssetDatabase.LoadAssetAtPath<Sprite>(Ui + sub);
        private static Sprite Gen(string file) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Sprites/Generated/" + file);

        private static readonly Color Steel = new(0.80f, 0.84f, 0.90f);
        private static readonly Color Gold = new(1f, 0.85f, 0.35f);
        private static readonly Color Ink = new(0.85f, 0.92f, 1f);

        [MenuItem("SpaceSurvivors/Build/M14a Shop scene")]
        private static void Build()
        {
            SetBorder("Upgrade/Window.png", 60, 120, 60, 120);
            SetBorder("Upgrade/Price_BTN_Table.png", 40, 40, 40, 40);
            SetBorder("Shop/Prise_BTN_Table.png", 40, 40, 40, 40);
            SetBorder("Main_UI/Stats_Bar.png", 24, 24, 24, 24);

            var cat = AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogue>("Assets/_Project/Resources/MetaUpgradeCatalogue.asset");
            if (cat == null) { Debug.LogError("[ShopBuilder] no MetaUpgradeCatalogue in Resources."); return; }
            var upgrades = cat.upgrades.Where(u => u != null).ToList();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // camera (solid dark, in case the BG image fails)
            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
            cam.orthographic = true;

            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // ---- canvas ----
            var canvasGo = new GameObject("ShopCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = Img("Background", canvasGo.transform, S("Main_Menu/BG.png"), Color.white, raycast: true);
            Stretch(bg.rectTransform);
            if (bg.sprite == null) bg.color = new Color(0.05f, 0.06f, 0.10f);

            var shop = canvasGo.AddComponent<ShopScreen>();

            // ---- main panel ----
            var panel = Img("Panel", canvasGo.transform, S("Upgrade/Window.png"), Color.white);
            panel.type = Image.Type.Sliced;
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(980, 900), new Vector2(0, 0));

            // Header.png carries the word "UPGRADE" — keep the plate narrow so its baked text
            // doesn't run into the wallet readout.
            var header = Img("Header", panel.transform, S("Upgrade/Header.png"), Color.white);
            Place(header, new Vector2(0.5f, 1f), new Vector2(300, 66), new Vector2(0, -52));

            // ---- wallet, its own row just under the header (never overlapping it) ----
            var wallet = new GameObject("WalletGroup", typeof(RectTransform));
            wallet.transform.SetParent(panel.transform, false);
            var wRt = (RectTransform)wallet.transform;
            wRt.anchorMin = wRt.anchorMax = wRt.pivot = new Vector2(0.5f, 1f);
            wRt.sizeDelta = new Vector2(260, 44);
            wRt.anchoredPosition = new Vector2(0, -104);
            var walletText = Label("WalletText", wallet.transform, "SCRAP  0", 26, Steel);
            walletText.fontStyle = FontStyle.Bold;
            walletText.alignment = TextAnchor.MiddleRight;
            var wtRt = walletText.rectTransform;
            wtRt.anchorMin = wtRt.anchorMax = wtRt.pivot = new Vector2(1f, 0.5f);
            wtRt.sizeDelta = new Vector2(210, 44);
            wtRt.anchoredPosition = new Vector2(-40, 0);
            var chip = Img("Chip", wallet.transform, Gen("ScrapChip.png"), Color.white);
            var chipRt = (RectTransform)chip.transform;
            chipRt.anchorMin = chipRt.anchorMax = chipRt.pivot = new Vector2(1f, 0.5f);
            chipRt.sizeDelta = new Vector2(34, 34);
            var wOutline = walletText.gameObject.AddComponent<Outline>();
            wOutline.effectColor = new Color(0.05f, 0.06f, 0.09f, 0.9f);
            wOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // ---- rows ----
            var rows = new List<ShopScreen.UpgradeRow>();
            const float rowH = 166f;
            const float top = 232f;   // y of the first row's centre, from panel centre (clears the wallet row)
            for (int i = 0; i < upgrades.Count; i++)
            {
                var u = upgrades[i];
                var row = Img($"Row_{u.id}", panel.transform, S("Main_UI/Stats_Bar.png"), new Color(1f, 1f, 1f, 0.85f));
                row.type = Image.Type.Sliced;
                Place(row, new Vector2(0.5f, 0.5f), new Vector2(860, rowH - 14), new Vector2(0, top - i * rowH));

                var icon = Img("Icon", row.transform, u.icon, Color.white);
                Place(icon, new Vector2(0f, 0.5f), new Vector2(96, 96), new Vector2(84, 4));

                var title = Label("Title", row.transform, u.title, 28, new Color(1f, 0.95f, 0.8f));
                title.alignment = TextAnchor.LowerLeft;
                Place(title, new Vector2(0f, 0.5f), new Vector2(430, 44), new Vector2(370, 34));

                var desc = Label("Desc", row.transform, u.description, 18, new Color(0.72f, 0.82f, 0.95f));
                desc.alignment = TextAnchor.UpperLeft;
                Place(desc, new Vector2(0f, 0.5f), new Vector2(430, 46), new Vector2(370, -14));

                var level = Label("Level", row.transform, "0 / " + u.maxLevel, 24, Gold);
                Place(level, new Vector2(1f, 0.5f), new Vector2(150, 40), new Vector2(-330, 34));

                // buy button
                var buyGo = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
                buyGo.transform.SetParent(row.transform, false);
                var buyImg = buyGo.GetComponent<Image>();
                buyImg.sprite = S("Upgrade/Price_BTN_Table.png");
                buyImg.type = Image.Type.Sliced;
                var buy = buyGo.GetComponent<Button>();
                var cb = buy.colors;
                cb.disabledColor = new Color(0.5f, 0.5f, 0.55f, 0.6f);
                cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
                buy.colors = cb;
                Place(buyGo.transform, new Vector2(1f, 0.5f), new Vector2(210, 96), new Vector2(-150, -8));

                var costChip = Img("CostChip", buyGo.transform, Gen("ScrapChip.png"), Color.white);
                Place(costChip, new Vector2(0f, 0.5f), new Vector2(34, 34), new Vector2(34, 0));
                var cost = Label("Cost", buyGo.transform, u.baseCost.ToString(), 26, new Color(0.97f, 0.95f, 0.85f));
                cost.fontStyle = FontStyle.Bold;
                Place(cost, new Vector2(0.5f, 0.5f), new Vector2(150, 50), new Vector2(18, 0));

                rows.Add(new ShopScreen.UpgradeRow
                {
                    upgradeId = u.id,
                    icon = icon,
                    title = title,
                    description = desc,
                    levelLabel = level,
                    costLabel = cost,
                    buyButton = buy,
                });
            }

            // ---- back button ----
            var back = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            back.transform.SetParent(panel.transform, false);
            var backImg = back.GetComponent<Image>();
            backImg.sprite = S("Shop/Prise_BTN_Table.png");
            backImg.type = Image.Type.Sliced;
            Place(back.transform, new Vector2(0.5f, 0f), new Vector2(300, 92), new Vector2(0, 54));
            var backLabel = Label("Label", back.transform, "BACK", 30, new Color(0.95f, 0.98f, 1f));
            Stretch(backLabel.rectTransform);

            // ---- wire ShopScreen ----
            var so = new SerializedObject(shop);
            so.FindProperty("_walletLabel").objectReferenceValue = walletText;
            so.FindProperty("_backButton").objectReferenceValue = back.GetComponent<Button>();
            so.FindProperty("_menuSceneName").stringValue = "MainMenu";
            var rowsProp = so.FindProperty("_rows");
            rowsProp.arraySize = rows.Count;
            for (int i = 0; i < rows.Count; i++)
            {
                var e = rowsProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("upgradeId").stringValue = rows[i].upgradeId;
                e.FindPropertyRelative("icon").objectReferenceValue = rows[i].icon;
                e.FindPropertyRelative("title").objectReferenceValue = rows[i].title;
                e.FindPropertyRelative("description").objectReferenceValue = rows[i].description;
                e.FindPropertyRelative("levelLabel").objectReferenceValue = rows[i].levelLabel;
                e.FindPropertyRelative("costLabel").objectReferenceValue = rows[i].costLabel;
                e.FindPropertyRelative("buyButton").objectReferenceValue = rows[i].buyButton;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            RegisterScene();
            Debug.Log($"[ShopBuilder] built {ScenePath} with {rows.Count} rows.");
        }

        [MenuItem("SpaceSurvivors/Build/M14a Main-Menu Shop button")]
        private static void BuildMenuButton()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

            // sit it just under the existing SETTINGS button, same parent / anchor / size
            var settings = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "SettingsButton");
            var parent = settings != null ? settings.transform.parent
                : Object.FindFirstObjectByType<Canvas>().transform;
            var srt = settings != null ? (RectTransform)settings.transform : null;

            var existing = parent.Find("ShopButton");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject("ShopButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LoadSceneButton));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = S("Shop/Prise_BTN_Table.png");
            img.type = Image.Type.Sliced;
            var rt = (RectTransform)go.transform;
            if (srt != null)
            {
                rt.anchorMin = srt.anchorMin; rt.anchorMax = srt.anchorMax; rt.pivot = srt.pivot;
                rt.sizeDelta = srt.sizeDelta;
                rt.anchoredPosition = srt.anchoredPosition + new Vector2(0, -(srt.sizeDelta.y + 14));
            }
            else
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
                rt.sizeDelta = new Vector2(230, 74);
                rt.anchoredPosition = new Vector2(-40, -128);
            }

            var label = Label("Label", go.transform, "SHOP", 30, new Color(0.95f, 0.98f, 1f));
            Stretch(label.rectTransform);

            var so = new SerializedObject(go.GetComponent<LoadSceneButton>());
            so.FindProperty("_sceneName").stringValue = "Shop";
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ShopBuilder] added SHOP button to MainMenu.");
        }

        private static void RegisterScene()
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == ScenePath)) return;
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
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
