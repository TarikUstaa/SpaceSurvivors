using System.Linq;
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
    /// Builds <c>Assets/_Project/Scenes/Hangar.unity</c> (M14b — ship shop) — a one-ship
    /// carousel over the ship catalogue. Editor tool because the RunCommand assembly can't
    /// reference UnityEngine.UI. Idempotent; also registers the scene and adds a HANGAR
    /// button to the main menu.
    /// </summary>
    internal static class HangarBuilder
    {
        private const string Ui = "Assets/_Project/Art/UI/PNG/";
        private const string ScenePath = "Assets/_Project/Scenes/Hangar.unity";
        private static Font Legacy => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static Sprite S(string sub) => AssetDatabase.LoadAssetAtPath<Sprite>(Ui + sub);
        private static Sprite Gen(string f) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Sprites/Generated/" + f);

        private static readonly Color Steel = new(0.80f, 0.84f, 0.90f);
        private static readonly Color Gold = new(1f, 0.85f, 0.35f);

        [MenuItem("SpaceSurvivors/Build/M14b Hangar scene")]
        private static void Build()
        {
            SetBorder("Ship_Shop/Window.png", 60, 120, 60, 120);
            SetBorder("Upgrade/Price_BTN_Table.png", 40, 40, 40, 40);
            SetBorder("Shop/Prise_BTN_Table.png", 40, 40, 40, 40);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
            cam.orthographic = true;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("HangarCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = Img("Background", canvasGo.transform, S("Main_Menu/BG.png"), Color.white, raycast: true);
            Stretch(bg.rectTransform);
            if (bg.sprite == null) bg.color = new Color(0.05f, 0.06f, 0.10f);

            var hangar = canvasGo.AddComponent<HangarScreen>();

            var panel = Img("Panel", canvasGo.transform, S("Ship_Shop/Window.png"), Color.white);
            panel.type = Image.Type.Sliced;
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(980, 940), Vector2.zero);

            var header = Img("Header", panel.transform, S("Ship_Shop/Header.png"), Color.white);
            Place(header, new Vector2(0.5f, 1f), new Vector2(320, 66), new Vector2(0, -52));

            // wallet — its own centred row under the header (the Header sprite has "SHIP SHOP"
            // baked in, so keeping it clear avoids the text running together)
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

            // ship image (centre)
            var shipImg = Img("ShipImage", panel.transform, null, Color.white);
            Place(shipImg, new Vector2(0.5f, 0.5f), new Vector2(340, 340), new Vector2(0, 140));

            // prev / next arrows flanking the ship
            var prev = ArrowButton("PrevButton", panel.transform, S("Ship_Shop/Backward_BTN.png"), new Vector2(-360, 140));
            var next = ArrowButton("NextButton", panel.transform, S("Ship_Shop/Forward_BTN.png"), new Vector2(360, 140));

            var name = Label("Name", panel.transform, "Scout", 40, new Color(1f, 0.96f, 0.82f));
            Place(name, new Vector2(0.5f, 0.5f), new Vector2(700, 54), new Vector2(0, -70));

            var desc = Label("Desc", panel.transform, "", 20, new Color(0.72f, 0.82f, 0.95f));
            Place(desc, new Vector2(0.5f, 0.5f), new Vector2(760, 44), new Vector2(0, -118));

            // Top-aligned so Ronin's three modifier lines grow down from a fixed point; the
            // action button is pushed lower (MetaScreenSkinner primaryButtonY) to make room.
            var stats = Label("Stats", panel.transform, "", 22, Gold);
            stats.alignment = TextAnchor.UpperCenter;
            stats.lineSpacing = 1f;
            Place(stats, new Vector2(0.5f, 0.5f), new Vector2(760, 130), new Vector2(0, -188));

            // action button
            var actionGo = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
            actionGo.transform.SetParent(panel.transform, false);
            var actionImg = actionGo.GetComponent<Image>();
            actionImg.sprite = S("Upgrade/Price_BTN_Table.png");
            actionImg.type = Image.Type.Sliced;
            var actionBtn = actionGo.GetComponent<Button>();
            var cb = actionBtn.colors;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.55f, 0.6f);
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            actionBtn.colors = cb;
            Place(actionGo.transform, new Vector2(0.5f, 0f), new Vector2(320, 104), new Vector2(0, 150));
            var actionChip = Img("ActionChip", actionGo.transform, Gen("ScrapChip.png"), Color.white);
            Place(actionChip, new Vector2(0f, 0.5f), new Vector2(36, 36), new Vector2(40, 0));
            var actionLabel = Label("ActionLabel", actionGo.transform, "0", 30, new Color(0.97f, 0.95f, 0.85f));
            actionLabel.fontStyle = FontStyle.Bold;
            Place(actionLabel, new Vector2(0.5f, 0.5f), new Vector2(220, 50), new Vector2(16, 0));

            // back button
            var back = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            back.transform.SetParent(panel.transform, false);
            var backImg = back.GetComponent<Image>();
            backImg.sprite = S("Shop/Prise_BTN_Table.png");
            backImg.type = Image.Type.Sliced;
            Place(back.transform, new Vector2(0.5f, 0f), new Vector2(280, 90), new Vector2(0, 40));
            var backLabel = Label("Label", back.transform, "BACK", 28, new Color(0.95f, 0.98f, 1f));
            Stretch(backLabel.rectTransform);

            // wire
            var so = new SerializedObject(hangar);
            so.FindProperty("_walletLabel").objectReferenceValue = walletText;
            so.FindProperty("_shipImage").objectReferenceValue = shipImg;
            so.FindProperty("_nameLabel").objectReferenceValue = name;
            so.FindProperty("_descLabel").objectReferenceValue = desc;
            so.FindProperty("_statsLabel").objectReferenceValue = stats;
            so.FindProperty("_prevButton").objectReferenceValue = prev;
            so.FindProperty("_nextButton").objectReferenceValue = next;
            so.FindProperty("_actionButton").objectReferenceValue = actionBtn;
            so.FindProperty("_actionLabel").objectReferenceValue = actionLabel;
            so.FindProperty("_actionChip").objectReferenceValue = actionChip.gameObject;
            so.FindProperty("_backButton").objectReferenceValue = back.GetComponent<Button>();
            so.FindProperty("_menuSceneName").stringValue = "MainMenu";
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScene();
            Debug.Log($"[HangarBuilder] built {ScenePath}");
        }

        [MenuItem("SpaceSurvivors/Build/M14b Main-Menu Hangar button")]
        private static void BuildMenuButton()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

            var shop = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "ShopButton");
            var settings = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "SettingsButton");
            var anchor = shop != null ? (RectTransform)shop.transform : settings != null ? (RectTransform)settings.transform : null;
            var parent = anchor != null ? anchor.parent : Object.FindFirstObjectByType<Canvas>().transform;

            var existing = parent.Find("HangarButton");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject("HangarButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LoadSceneButton));
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
                rt.anchoredPosition = new Vector2(-40, -216);
            }

            var label = Label("Label", go.transform, "HANGAR", 28, new Color(0.95f, 0.98f, 1f));
            Stretch(label.rectTransform);

            var lso = new SerializedObject(go.GetComponent<LoadSceneButton>());
            lso.FindProperty("_sceneName").stringValue = "Hangar";
            lso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HangarBuilder] added HANGAR button to MainMenu.");
        }

        private static void RegisterScene()
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == ScenePath)) return;
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ---------------------------------------------------------------- primitives

        private static Button ArrowButton(string name, Transform parent, Sprite sprite, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var im = go.GetComponent<Image>();
            im.sprite = sprite;
            var b = go.GetComponent<Button>();
            var cb = b.colors;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.55f, 0.5f);
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            b.colors = cb;
            Place(go.transform, new Vector2(0.5f, 0.5f), new Vector2(96, 96), pos);
            return b;
        }

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
