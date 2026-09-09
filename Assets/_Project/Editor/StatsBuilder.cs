using System.Linq;
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
    /// Builds <c>Assets/_Project/Scenes/Stats.unity</c> — the career page, one screen for the
    /// lifetime numbers that used to be four cramped lines on the main-menu pilot card
    /// (which is now the leaderboard). Modelled on <see cref="ShopBuilder"/>: an editor tool
    /// because the runtime assemblies can't reference UnityEngine.UI, idempotent, recreates
    /// the scene each run and re-registers it in Build Settings.
    ///
    /// <para>Two menu items, same split every meta screen uses:
    /// <c>SpaceSurvivors/Build/Stats scene</c> builds the scene;
    /// <c>SpaceSurvivors/Build/Main-Menu Stats button</c> adds the nav button. Run both, then
    /// re-run the M20 main-menu redesign so the button gets its glass styling.</para>
    /// </summary>
    internal static class StatsBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Stats.unity";
        private const string Gen = "Assets/_Project/Art/Sprites/Generated/";

        private static Font Legacy => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static Sprite Panel => AssetDatabase.LoadAssetAtPath<Sprite>(Gen + "MenuPanel.png");

        private static readonly Color Ink = new(0.90f, 0.96f, 1f);
        private static readonly Color InkDim = new(0.64f, 0.78f, 0.92f);
        private static readonly Color Heading = new(0.75f, 0.90f, 1f);

        // label, serialized-field name on StatsScreen, section header (null = same section)
        private static readonly (string label, string field, string section)[] Lines =
        {
            ("Runs played",     "_runsValue",          "RUNS"),
            ("Best survival",   "_bestTimeValue",      null),
            ("Best level",      "_bestLevelValue",     null),
            ("Best kills",      "_bestKillsValue",     null),
            ("Total kills",     "_lifetimeKillsValue", "LIFETIME"),
            ("Bosses defeated", "_bossKillsValue",     null),
            ("Scrap earned",    "_lifetimeScrapValue", null),
            ("Scrap on hand",   "_walletValue",        "NOW"),
            ("Achievements",    "_achievementsValue",  null),
        };

        [MenuItem("SpaceSurvivors/Build/Stats scene")]
        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
            cam.orthographic = true;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("StatsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = Img("Background", canvasGo.transform, null, new Color(0.05f, 0.06f, 0.10f), raycast: true);
            Stretch(bg.rectTransform);

            var screen = canvasGo.AddComponent<StatsScreen>();

            // ---- window ----
            var window = Img("Window", canvasGo.transform, Panel, new Color(0.16f, 0.22f, 0.32f, 0.95f));
            window.type = Image.Type.Sliced;
            var wrt = window.rectTransform;
            wrt.anchorMin = wrt.anchorMax = wrt.pivot = new Vector2(0.5f, 0.5f);
            wrt.sizeDelta = new Vector2(720, 780);
            wrt.anchoredPosition = new Vector2(0f, 20f);

            // The screen title is added by MetaScreenSkinner ("CAREER", inside the window at
            // the top, matching HANGAR / UPGRADES / the rest). No separate title here.

            // ---- rows ----
            var so = new SerializedObject(screen);
            float y = 300f;
            foreach (var line in Lines)
            {
                if (line.section != null)
                {
                    y -= 14f;
                    var h = Label(line.section + "Head", window.transform, line.section, 20, Heading);
                    Row((RectTransform)h.transform, y, 0f);
                    ((Text)h).alignment = TextAnchor.MiddleLeft;
                    y -= 46f;
                }

                var l = Label(line.field + "Label", window.transform, line.label, 24, InkDim);
                Row((RectTransform)l.transform, y, 0f);
                l.alignment = TextAnchor.MiddleLeft;

                var v = Label(line.field + "Value", window.transform, "–", 26, Ink);
                Row((RectTransform)v.transform, y, 1f);
                v.alignment = TextAnchor.MiddleRight;

                so.FindProperty(line.field).objectReferenceValue = v;
                y -= 52f;
            }

            // ---- back ----
            var back = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            back.transform.SetParent(canvasGo.transform, false);
            var backImg = back.GetComponent<Image>();
            backImg.sprite = Panel;
            backImg.type = Image.Type.Sliced;
            backImg.color = new Color(0.6f, 0.84f, 1f, 0.92f);
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(260, 68);
            brt.anchoredPosition = new Vector2(0f, -440f);
            var backLabel = Label("Label", back.transform, "BACK", 26, new Color(0.96f, 0.99f, 1f));
            Stretch(backLabel.rectTransform);

            so.FindProperty("_backButton").objectReferenceValue = back.GetComponent<Button>();
            so.FindProperty("_menuSceneName").stringValue = "MainMenu";
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScene();
            Debug.Log($"[StatsBuilder] built {ScenePath}.");
        }

        [MenuItem("SpaceSurvivors/Build/Main-Menu Stats button")]
        private static void BuildMenuButton()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

            // Sit it next to the Hangar button, cloning its transform — the M20 redesign
            // re-parents all of these into the MetaRow and restyles them afterwards.
            var anchor = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "HangarButton")
                ?? Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.name == "ShopButton");
            var parent = anchor != null ? anchor.transform.parent
                : Object.FindFirstObjectByType<Canvas>().transform;

            var existing = parent.Find("StatsButton");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject("StatsButton",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LoadSceneButton));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().type = Image.Type.Sliced;

            var rt = (RectTransform)go.transform;
            if (anchor != null)
            {
                var art = (RectTransform)anchor.transform;
                rt.anchorMin = art.anchorMin; rt.anchorMax = art.anchorMax; rt.pivot = art.pivot;
                rt.sizeDelta = art.sizeDelta;
                rt.anchoredPosition = art.anchoredPosition + new Vector2(0f, -(art.sizeDelta.y + 14f));
            }
            else
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
                rt.sizeDelta = new Vector2(230, 74);
                rt.anchoredPosition = new Vector2(-40, -200);
            }

            var label = Label("Label", go.transform, "STATS", 30, new Color(0.95f, 0.98f, 1f));
            Stretch(label.rectTransform);

            var lso = new SerializedObject(go.GetComponent<LoadSceneButton>());
            lso.FindProperty("_sceneName").stringValue = "Stats";
            lso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[StatsBuilder] added STATS button to MainMenu. Re-run the M20 redesign to style it.");
        }

        // ---------------------------------------------------------------- primitives

        private static void Row(RectTransform rt, float y, float side)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(side, 0.5f);
            rt.pivot = new Vector2(side, 0.5f);
            rt.sizeDelta = new Vector2(360f, 40f);
            rt.anchoredPosition = new Vector2(side == 0f ? 44f : -44f, y);
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

        private static void RegisterScene()
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == ScenePath)) return;
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
