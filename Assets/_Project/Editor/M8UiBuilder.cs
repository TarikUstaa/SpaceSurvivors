using SpaceSurvivors.Data;
using SpaceSurvivors.Game;
using SpaceSurvivors.Enemies;
using SpaceSurvivors.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// One-shot builders for the M8 menu + end-of-run UI. Run from the menu; they assemble
    /// real uGUI hierarchies with the CraftPix kit sprites and wire the components. Kept as
    /// an editor tool because the Unity-MCP RunCommand assembly can't reference UnityEngine.UI.
    /// </summary>
    internal static class M8UiBuilder
    {
        private const string UiRoot = "Assets/_Project/Art/UI/PNG/";
        private static Font Legacy => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static Sprite Spr(string sub) => AssetDatabase.LoadAssetAtPath<Sprite>(UiRoot + sub);

        // ---------------------------------------------------------------- helpers

        private static Canvas NewCanvas(string name, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            return c;
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
            return t;
        }

        private static Button Btn(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().sprite = sprite;
            var b = go.GetComponent<Button>();
            b.transition = Selectable.Transition.ColorTint;
            var cb = b.colors;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            cb.pressedColor = new Color(0.8f, 0.85f, 0.95f);
            b.colors = cb;
            return b;
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

        private static void EnsureEventSystem(Scene scene)
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EditorSceneManager.MoveGameObjectToScene(go, scene);
        }

        // ---------------------------------------------------------------- Run-end screen

        [MenuItem("SpaceSurvivors/Build/Run-End Screen (Game scene)")]
        private static void BuildRunEndScreen()
        {
            var scene = EditorSceneManager.GetActiveScene().path == "Assets/_Project/Scenes/Game.unity"
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene("Assets/_Project/Scenes/Game.unity", OpenSceneMode.Single);
            GameObject ui = null, systems = null;
            foreach (var g in scene.GetRootGameObjects())
            {
                if (g.name == "UI") ui = g;
                if (g.name == "Systems") systems = g;
            }
            if (ui == null || systems == null) { Debug.LogError("UI / Systems object not found in active scene"); return; }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(ui);
            var prior = ui.transform.Find("RunEndCanvas");
            if (prior != null) Object.DestroyImmediate(prior.gameObject);

            var canvas = NewCanvas("RunEndCanvas", ui.transform, 500);

            var dim = Img("Dim", canvas.transform, null, new Color(0f, 0f, 0f, 0.72f), raycast: true);
            Stretch((RectTransform)dim.transform);

            var window = Img("Window", dim.transform, Spr("You_Win/Window.png"), Color.white);
            window.type = Image.Type.Sliced;
            Place(window, new Vector2(0.5f, 0.5f), new Vector2(680, 820), Vector2.zero);

            var header = Img("Header", window.transform, Spr("You_Win/Header.png"), Color.white);
            Place(header, new Vector2(0.5f, 1f), new Vector2(470, 100), new Vector2(0, -6));

            var scoreLabel = Img("ScoreLabel", window.transform, Spr("You_Win/Score.png"), Color.white);
            Place(scoreLabel, new Vector2(0.5f, 0.6f), new Vector2(260, 64), Vector2.zero);

            var value = Label("ScoreValue", window.transform, "0:00", 76, new Color(0.9f, 0.97f, 1f));
            Place(value, new Vector2(0.5f, 0.44f), new Vector2(420, 110), Vector2.zero);

            var replay = Btn("ReplayButton", window.transform, Spr("You_Win/Replay_BTN.png"));
            Place(replay, new Vector2(0.5f, 0.16f), new Vector2(120, 120), new Vector2(-95, 0));

            var menu = Btn("MenuButton", window.transform, Spr("Buttons/BTNs/Menu_BTN.png"));
            Place(menu, new Vector2(0.5f, 0.16f), new Vector2(120, 120), new Vector2(95, 0));

            var screen = canvas.gameObject.AddComponent<RunEndScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("_run").objectReferenceValue = systems.GetComponent<RunController>();
            so.FindProperty("_root").objectReferenceValue = dim.gameObject;
            so.FindProperty("_headerImage").objectReferenceValue = header;
            so.FindProperty("_winHeader").objectReferenceValue = Spr("You_Win/Header.png");
            so.FindProperty("_loseHeader").objectReferenceValue = Spr("You_Lose/Header.png");
            so.FindProperty("_scoreValue").objectReferenceValue = value;
            so.FindProperty("_replayButton").objectReferenceValue = replay;
            so.FindProperty("_menuButton").objectReferenceValue = menu;
            so.FindProperty("_menuSceneName").stringValue = "MainMenu";
            so.ApplyModifiedPropertiesWithoutUndo();

            dim.gameObject.SetActive(false);

            var rc = systems.GetComponent<RunController>();
            var rcso = new SerializedObject(rc);
            rcso.FindProperty("_spawnDirector").objectReferenceValue = systems.GetComponent<SpawnDirector>();
            rcso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[M8] Run-End screen built + wired.");
        }

        // ---------------------------------------------------------------- Main menu

        [MenuItem("SpaceSurvivors/Build/Main Menu (MainMenu scene)")]
        private static void BuildMainMenu()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

            foreach (var g in scene.GetRootGameObjects())
                if (g.name is "MenuRoot" or "EventSystem")
                    Object.DestroyImmediate(g);

            EnsureEventSystem(scene);

            var root = new GameObject("MenuRoot");
            EditorSceneManager.MoveGameObjectToScene(root, scene);
            var canvas = NewCanvas("MenuCanvas", root.transform, 0);

            var bg = Img("BG", canvas.transform, Spr("Main_Menu/BG.png"), Color.white);
            Stretch((RectTransform)bg.transform);
            bg.type = Image.Type.Simple;
            bg.preserveAspect = false;

            var title = Label("Title", canvas.transform, "SPACE  SURVIVORS", 78, new Color(0.8f, 0.96f, 1f));
            title.fontStyle = FontStyle.Bold;
            Place(title, new Vector2(0.5f, 0.8f), new Vector2(1400, 140), Vector2.zero);

            var subtitle = Label("Subtitle", canvas.transform, "SELECT A MODE", 30, new Color(0.6f, 0.8f, 0.92f));
            Place(subtitle, new Vector2(0.5f, 0.68f), new Vector2(600, 50), Vector2.zero);

            Sprite btnSlab = Spr("Shop/Prise_BTN_Table.png");

            var campaign = Btn("CampaignButton", canvas.transform, btnSlab);
            campaign.image.type = Image.Type.Sliced;
            Place(campaign, new Vector2(0.5f, 0.52f), new Vector2(460, 130), Vector2.zero);
            Stretch(Label("CampaignText", campaign.transform, "CAMPAIGN", 40, new Color(0.92f, 0.98f, 1f)).rectTransform);

            var infinite = Btn("InfiniteButton", canvas.transform, btnSlab);
            infinite.image.type = Image.Type.Sliced;
            Place(infinite, new Vector2(0.5f, 0.37f), new Vector2(460, 130), Vector2.zero);
            Stretch(Label("InfiniteText", infinite.transform, "INFINITE", 40, new Color(0.92f, 0.98f, 1f)).rectTransform);

            var desc = Label("Description", canvas.transform, "", 26, new Color(0.72f, 0.86f, 0.96f));
            Place(desc, new Vector2(0.5f, 0.24f), new Vector2(1000, 70), Vector2.zero);

            var quit = Btn("QuitButton", canvas.transform, Spr("Main_Menu/Exit_BTN.png"));
            Place(quit, new Vector2(0.5f, 0.1f), new Vector2(110, 110), Vector2.zero);

            var menuScreen = root.AddComponent<MainMenuScreen>();
            var so = new SerializedObject(menuScreen);
            var modes = so.FindProperty("_modes");
            modes.arraySize = 2;
            modes.GetArrayElementAtIndex(0).FindPropertyRelative("button").objectReferenceValue = campaign;
            modes.GetArrayElementAtIndex(0).FindPropertyRelative("mode").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameModeData>("Assets/_Project/ScriptableObjects/Config/Mode_Campaign.asset");
            modes.GetArrayElementAtIndex(1).FindPropertyRelative("button").objectReferenceValue = infinite;
            modes.GetArrayElementAtIndex(1).FindPropertyRelative("mode").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameModeData>("Assets/_Project/ScriptableObjects/Config/Mode_Infinite.asset");
            so.FindProperty("_gameSceneName").stringValue = "Game";
            so.FindProperty("_quitButton").objectReferenceValue = quit;
            so.FindProperty("_descriptionLabel").objectReferenceValue = desc;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[M8] Main menu built + wired.");
        }
    }
}
