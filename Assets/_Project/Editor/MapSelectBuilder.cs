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
    /// Builds <c>Assets/_Project/Scenes/MapSelect.unity</c> (M15) — a one-map carousel over the
    /// map catalogue — and adds a MAPS button to the main menu. Editor tool because the
    /// RunCommand assembly can't reference UnityEngine.UI. Idempotent; registers the scene.
    /// </summary>
    internal static class MapSelectBuilder
    {
        private const string Ui = "Assets/_Project/Art/UI/PNG/";
        private const string ScenePath = "Assets/_Project/Scenes/MapSelect.unity";
        private static Font Legacy => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static Sprite S(string sub) => AssetDatabase.LoadAssetAtPath<Sprite>(Ui + sub);

        [MenuItem("SpaceSurvivors/Build/M15 Map-select scene")]
        private static void Build()
        {
            SetBorder("Ship_Shop/Window.png", 60, 120, 60, 120);
            SetBorder("Shop/Prise_BTN_Table.png", 40, 40, 40, 40);
            SetBorder("Upgrade/Price_BTN_Table.png", 40, 40, 40, 40);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
            cam.orthographic = true;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("MapSelectCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = Img("Background", canvasGo.transform, S("Main_Menu/BG.png"), Color.white, raycast: true);
            Stretch(bg.rectTransform);
            if (bg.sprite == null) bg.color = new Color(0.05f, 0.06f, 0.10f);

            var screen = canvasGo.AddComponent<MapSelectScreen>();

            var panel = Img("Panel", canvasGo.transform, S("Ship_Shop/Window.png"), Color.white);
            panel.type = Image.Type.Sliced;
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(980, 940), Vector2.zero);

            // The panel art has its own dark title bar at the top — drop a plain label on it
            // (the Ship_Shop Header.png has "SHIP SHOP" baked in, so we don't use it here).
            var headerText = Label("HeaderText", panel.transform, "SELECT MAP", 40, new Color(0.96f, 0.98f, 1f));
            headerText.fontStyle = FontStyle.Bold;
            Place(headerText, new Vector2(0.5f, 1f), new Vector2(600, 70), new Vector2(0, -52));

            // preview swatch / art
            var preview = Img("Preview", panel.transform, null, Color.white);
            Place(preview, new Vector2(0.5f, 0.5f), new Vector2(560, 300), new Vector2(0, 150));
            var previewFrame = Img("PreviewFrame", panel.transform, S("Ship_Shop/Table_01.png"), Color.white);
            previewFrame.type = Image.Type.Sliced;
            Place(previewFrame, new Vector2(0.5f, 0.5f), new Vector2(600, 340), new Vector2(0, 150));
            preview.transform.SetAsLastSibling();

            var prev = ArrowButton("PrevButton", panel.transform, S("Ship_Shop/Backward_BTN.png"), new Vector2(-372, 150));
            var next = ArrowButton("NextButton", panel.transform, S("Ship_Shop/Forward_BTN.png"), new Vector2(372, 150));

            var name = Label("Name", panel.transform, "Milky Way", 42, new Color(1f, 0.96f, 0.82f));
            name.fontStyle = FontStyle.Bold;
            Place(name, new Vector2(0.5f, 0.5f), new Vector2(760, 56), new Vector2(0, -60));

            var desc = Label("Desc", panel.transform, "", 22, new Color(0.75f, 0.84f, 0.96f));
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(desc, new Vector2(0.5f, 0.5f), new Vector2(780, 120), new Vector2(0, -150));

            // PLAY — commits the map choice and loads the game
            var playGo = new GameObject("PlayButton", typeof(RectTransform), typeof(Image), typeof(Button));
            playGo.transform.SetParent(panel.transform, false);
            var playImg = playGo.GetComponent<Image>();
            playImg.sprite = S("Upgrade/Price_BTN_Table.png");
            playImg.type = Image.Type.Sliced;
            var playBtn = playGo.GetComponent<Button>();
            var cb = playBtn.colors;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.55f, 0.6f);
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            playBtn.colors = cb;
            Place(playGo.transform, new Vector2(0.5f, 0f), new Vector2(340, 108), new Vector2(0, 152));
            var playLabel = Label("PlayLabel", playGo.transform, "PLAY", 34, new Color(0.97f, 0.95f, 0.85f));
            playLabel.fontStyle = FontStyle.Bold;
            Stretch(playLabel.rectTransform);

            var back = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            back.transform.SetParent(panel.transform, false);
            var backImg = back.GetComponent<Image>();
            backImg.sprite = S("Shop/Prise_BTN_Table.png");
            backImg.type = Image.Type.Sliced;
            Place(back.transform, new Vector2(0.5f, 0f), new Vector2(280, 90), new Vector2(0, 42));
            var backLabel = Label("Label", back.transform, "BACK", 28, new Color(0.95f, 0.98f, 1f));
            Stretch(backLabel.rectTransform);

            var so = new SerializedObject(screen);
            so.FindProperty("_preview").objectReferenceValue = preview;
            so.FindProperty("_nameLabel").objectReferenceValue = name;
            so.FindProperty("_descLabel").objectReferenceValue = desc;
            so.FindProperty("_prevButton").objectReferenceValue = prev;
            so.FindProperty("_nextButton").objectReferenceValue = next;
            so.FindProperty("_playButton").objectReferenceValue = playBtn;
            so.FindProperty("_backButton").objectReferenceValue = back.GetComponent<Button>();
            so.FindProperty("_menuSceneName").stringValue = "MainMenu";
            so.FindProperty("_gameSceneName").stringValue = "Game";
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScene();
            Debug.Log($"[MapSelectBuilder] built {ScenePath}");
        }

        [MenuItem("SpaceSurvivors/Build/M15 Route Main-Menu through Map-select")]
        private static void RouteMainMenu()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

            // No standalone MAPS section — the map picker opens after choosing a mode.
            var stray = Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .FirstOrDefault(b => b.name == "MapsButton");
            if (stray != null) Object.DestroyImmediate(stray.gameObject);

            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            if (menu != null)
            {
                var mso = new SerializedObject(menu);
                mso.FindProperty("_gameSceneName").stringValue = "MapSelect";
                mso.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[MapSelectBuilder] no MainMenuScreen found.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MapSelectBuilder] Campaign/Infinite now load MapSelect; MAPS button removed.");
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
            go.GetComponent<Image>().sprite = sprite;
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
