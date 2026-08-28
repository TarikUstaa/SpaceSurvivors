using SpaceSurvivors.Enemies;
using SpaceSurvivors.Game;
using SpaceSurvivors.Progression;
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
    /// M10 wave-1 UI: pause screen (+ in-game settings panel), run-stats / best-time rows on
    /// the run-end screen, and the Campaign "STAGE n/N" readout. Editor tool because the
    /// Unity-MCP RunCommand assembly can't reference UnityEngine.UI. Idempotent — safe to
    /// re-run; it destroys its prior output first.
    /// </summary>
    internal static class M10UiBuilder
    {
        private const string Ui = "Assets/_Project/Art/UI/PNG/";
        private static Font Legacy => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static Sprite S(string sub) => AssetDatabase.LoadAssetAtPath<Sprite>(Ui + sub);

        [MenuItem("SpaceSurvivors/Build/M10 UI (Game scene)")]
        private static void Build()
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
            if (ui == null || systems == null) { Debug.LogError("[M10] UI / Systems root missing"); return; }

            SetBorder("Pause/Window.png", 48, 48, 48, 60);
            SetBorder("Setting/Window.png", 48, 48, 48, 60);

            EnsureEventSystem(scene);
            var runStats = AddRunStats(systems);
            BuildPause(ui, systems);
            AddStageIndicator(ui, systems);
            ExtendRunEnd(ui, systems, runStats);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[M10] wave-1 UI built + wired.");
        }

        // ---------------------------------------------------------------- RunStats

        private static RunStats AddRunStats(GameObject systems)
        {
            var rs = systems.GetComponent<RunStats>() ?? systems.AddComponent<RunStats>();
            var so = new SerializedObject(rs);
            so.FindProperty("_spawnDirector").objectReferenceValue = systems.GetComponent<SpawnDirector>();
            so.FindProperty("_levelSystem").objectReferenceValue = Object.FindFirstObjectByType<LevelSystem>();
            so.FindProperty("_scrapCollector").objectReferenceValue = Object.FindFirstObjectByType<ScrapCollector>();
            so.FindProperty("_clock").objectReferenceValue = Object.FindFirstObjectByType<SpaceSurvivors.Core.RunClock>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return rs;
        }

        // ---------------------------------------------------------------- Pause + settings

        private static void BuildPause(GameObject ui, GameObject systems)
        {
            var prior = ui.transform.Find("PauseCanvas");
            if (prior != null) Object.DestroyImmediate(prior.gameObject);

            var canvas = Canvas("PauseCanvas", ui.transform, 600);
            var dim = Img("Dim", canvas.transform, null, new Color(0f, 0f, 0f, 0.78f), raycast: true);
            Stretch((RectTransform)dim.transform);

            // ---- main group
            var main = new GameObject("MainGroup", typeof(RectTransform));
            main.transform.SetParent(dim.transform, false);
            Stretch((RectTransform)main.transform);

            var window = Img("Window", main.transform, S("Pause/Window.png"), Color.white);
            window.type = Image.Type.Sliced;
            Place(window, new Vector2(0.5f, 0.5f), new Vector2(600, 640), new Vector2(320, 0));

            var header = Img("Header", window.transform, S("Pause/Header.png"), Color.white);
            Place(header, new Vector2(0.5f, 1f), new Vector2(420, 96), new Vector2(0, 8));

            var resume = TextButton("ResumeButton", window.transform, S("Shop/Prise_BTN_Table.png"), "RESUME");
            Place(resume, new Vector2(0.5f, 0.66f), new Vector2(420, 108), Vector2.zero);
            var settings = TextButton("SettingsButton", window.transform, S("Shop/Prise_BTN_Table.png"), "SETTINGS");
            Place(settings, new Vector2(0.5f, 0.44f), new Vector2(420, 108), Vector2.zero);
            var menu = TextButton("MenuButton", window.transform, S("Shop/Prise_BTN_Table.png"), "MAIN MENU");
            Place(menu, new Vector2(0.5f, 0.22f), new Vector2(420, 108), Vector2.zero);

            // ---- ship stats column, left of the pause window
            var statsWin = Img("StatsColumn", main.transform, S("Setting/Window.png"), Color.white);
            statsWin.type = Image.Type.Sliced;
            Place(statsWin, new Vector2(0.5f, 0.5f), new Vector2(520, 780), new Vector2(-380, 0));

            var statsTitle = Label("Title", statsWin.transform, "SHIP  STATUS", 26, new Color(0.8f, 0.94f, 1f));
            statsTitle.fontStyle = FontStyle.Bold;
            Place(statsTitle, new Vector2(0.5f, 1f), new Vector2(420, 50), new Vector2(0, -40));

            var statLabels = Label("Labels", statsWin.transform, "", 20, new Color(0.74f, 0.86f, 0.97f));
            statLabels.alignment = TextAnchor.UpperLeft;
            statLabels.supportRichText = true;
            statLabels.lineSpacing = 1.05f;
            var slRt = statLabels.rectTransform;
            slRt.anchorMin = new Vector2(0f, 0f); slRt.anchorMax = new Vector2(1f, 1f);
            slRt.offsetMin = new Vector2(44, 40); slRt.offsetMax = new Vector2(-180, -96);

            var statValues = Label("Values", statsWin.transform, "", 20, new Color(0.98f, 0.95f, 0.8f));
            statValues.alignment = TextAnchor.UpperRight;
            statValues.lineSpacing = 1.05f;
            var svRt = statValues.rectTransform;
            svRt.anchorMin = new Vector2(0f, 0f); svRt.anchorMax = new Vector2(1f, 1f);
            svRt.offsetMin = new Vector2(44, 40); svRt.offsetMax = new Vector2(-44, -96);

            var statsPanel = statsWin.gameObject.AddComponent<StatsPanel>();
            var playerStats = Object.FindFirstObjectByType<SpaceSurvivors.Stats.StatSheet>();
            var stso = new SerializedObject(statsPanel);
            stso.FindProperty("_stats").objectReferenceValue = playerStats;
            stso.FindProperty("_playerConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SpaceSurvivors.Data.PlayerConfig>("Assets/_Project/ScriptableObjects/Config/PlayerConfig.asset");
            stso.FindProperty("_playerHealth").objectReferenceValue = playerStats != null ? playerStats.GetComponent<SpaceSurvivors.Combat.HealthComponent>() : null;
            stso.FindProperty("_shield").objectReferenceValue = playerStats != null ? playerStats.GetComponent<SpaceSurvivors.Combat.ShieldComponent>() : null;
            stso.FindProperty("_level").objectReferenceValue = Object.FindFirstObjectByType<SpaceSurvivors.Progression.LevelSystem>();
            stso.FindProperty("_runStats").objectReferenceValue = systems.GetComponent<RunStats>();
            stso.FindProperty("_weapons").objectReferenceValue = Object.FindFirstObjectByType<SpaceSurvivors.Combat.WeaponController>();
            stso.FindProperty("_labels").objectReferenceValue = statLabels;
            stso.FindProperty("_values").objectReferenceValue = statValues;
            stso.ApplyModifiedPropertiesWithoutUndo();

            // ---- settings group
            var settingsGroup = new GameObject("SettingsGroup", typeof(RectTransform));
            settingsGroup.transform.SetParent(dim.transform, false);
            Stretch((RectTransform)settingsGroup.transform);

            var sWindow = Img("Window", settingsGroup.transform, S("Setting/Window.png"), Color.white);
            sWindow.type = Image.Type.Sliced;
            Place(sWindow, new Vector2(0.5f, 0.5f), new Vector2(720, 660), Vector2.zero);

            var sHeader = Img("Header", sWindow.transform, S("Setting/Header.png"), Color.white);
            Place(sHeader, new Vector2(0.5f, 1f), new Vector2(430, 96), new Vector2(0, 8));

            var master = SliderRow(sWindow.transform, "Master", "MASTER", 0.66f, out var masterVal);
            var music  = SliderRow(sWindow.transform, "Music",  "MUSIC",  0.50f, out var musicVal);
            var sfx    = SliderRow(sWindow.transform, "Sfx",    "SFX",    0.34f, out var sfxVal);
            var fs     = ToggleRow(sWindow.transform, "Fullscreen", "FULLSCREEN", 0.18f);

            var back = TextButton("BackButton", sWindow.transform, S("Shop/Prise_BTN_Table.png"), "BACK");
            Place(back, new Vector2(0.5f, 0.06f), new Vector2(300, 92), Vector2.zero);

            var panel = settingsGroup.AddComponent<SettingsPanel>();
            var pso = new SerializedObject(panel);
            pso.FindProperty("_masterSlider").objectReferenceValue = master;
            pso.FindProperty("_musicSlider").objectReferenceValue = music;
            pso.FindProperty("_sfxSlider").objectReferenceValue = sfx;
            pso.FindProperty("_fullscreenToggle").objectReferenceValue = fs;
            pso.FindProperty("_masterValue").objectReferenceValue = masterVal;
            pso.FindProperty("_musicValue").objectReferenceValue = musicVal;
            pso.FindProperty("_sfxValue").objectReferenceValue = sfxVal;
            pso.ApplyModifiedPropertiesWithoutUndo();

            var pause = canvas.gameObject.AddComponent<PauseScreen>();
            var pauseSo = new SerializedObject(pause);
            pauseSo.FindProperty("_run").objectReferenceValue = systems.GetComponent<RunController>();
            pauseSo.FindProperty("_root").objectReferenceValue = dim.gameObject;
            pauseSo.FindProperty("_mainGroup").objectReferenceValue = main;
            pauseSo.FindProperty("_settingsRoot").objectReferenceValue = settingsGroup;
            pauseSo.FindProperty("_resumeButton").objectReferenceValue = resume;
            pauseSo.FindProperty("_settingsButton").objectReferenceValue = settings;
            pauseSo.FindProperty("_settingsBackButton").objectReferenceValue = back;
            pauseSo.FindProperty("_menuButton").objectReferenceValue = menu;
            pauseSo.FindProperty("_menuSceneName").stringValue = "MainMenu";
            pauseSo.ApplyModifiedPropertiesWithoutUndo();

            dim.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- Stage indicator

        private static void AddStageIndicator(GameObject ui, GameObject systems)
        {
            var prior = ui.transform.Find("StageCanvas");
            if (prior != null) Object.DestroyImmediate(prior.gameObject);

            var canvas = Canvas("StageCanvas", ui.transform, 120);
            var label = Label("StageLabel", canvas.transform, "STAGE 1/3", 28, new Color(1f, 0.86f, 0.55f));
            label.fontStyle = FontStyle.Bold;
            // Below the RunHud timer (timer sits at y -34..-84 from the top).
            Place(label, new Vector2(0.5f, 1f), new Vector2(360, 40), new Vector2(0, -122));

            var si = canvas.gameObject.AddComponent<StageIndicator>();
            var so = new SerializedObject(si);
            so.FindProperty("_spawnDirector").objectReferenceValue = systems.GetComponent<SpawnDirector>();
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_root").objectReferenceValue = canvas.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Run-end extras

        private static void ExtendRunEnd(GameObject ui, GameObject systems, RunStats runStats)
        {
            var canvas = ui.transform.Find("RunEndCanvas");
            if (canvas == null) { Debug.LogWarning("[M10] RunEndCanvas not found — run the M8 builder first."); return; }
            var window = canvas.Find("Dim/Window");
            if (window == null) { Debug.LogWarning("[M10] RunEnd Window not found"); return; }

            var existingStats = window.Find("StatsValue");
            if (existingStats != null) Object.DestroyImmediate(existingStats.gameObject);
            var existingBest = window.Find("BestValue");
            if (existingBest != null) Object.DestroyImmediate(existingBest.gameObject);

            // Re-flow the whole window so nothing overlaps now that stats + best are in it.
            Reposition(window, "Header",       new Vector2(0.5f, 1.00f), new Vector2(470, 100), new Vector2(0, -6));
            Reposition(window, "ScoreLabel",   new Vector2(0.5f, 0.74f), new Vector2(260, 60),  Vector2.zero);
            Reposition(window, "ScoreValue",   new Vector2(0.5f, 0.615f),new Vector2(420, 96),  Vector2.zero);
            Reposition(window, "ReplayButton", new Vector2(0.5f, 0.085f),new Vector2(120, 120), new Vector2(-95, 0));
            Reposition(window, "MenuButton",   new Vector2(0.5f, 0.085f),new Vector2(120, 120), new Vector2(95, 0));

            var stats = Label("StatsValue", window, "KILLS  0\nLEVEL  1\nSCRAP  0", 28, new Color(0.82f, 0.9f, 1f));
            stats.lineSpacing = 1.3f;
            Place(stats, new Vector2(0.5f, 0.42f), new Vector2(460, 150), Vector2.zero);

            var best = Label("BestValue", window, "BEST  0:00", 26, new Color(1f, 0.86f, 0.55f));
            Place(best, new Vector2(0.5f, 0.245f), new Vector2(460, 44), Vector2.zero);

            var screen = canvas.GetComponent<RunEndScreen>();
            if (screen != null)
            {
                var so = new SerializedObject(screen);
                so.FindProperty("_stats").objectReferenceValue = runStats;
                so.FindProperty("_statsValue").objectReferenceValue = stats;
                so.FindProperty("_bestValue").objectReferenceValue = best;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------------------------------------------------------------- widgets

        private static Slider SliderRow(Transform parent, string name, string caption, float anchorY, out Text valueLabel)
        {
            var row = new GameObject(name + "Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            Place(row.transform, new Vector2(0.5f, anchorY), new Vector2(600, 70), Vector2.zero);

            var cap = Label(name + "Caption", row.transform, caption, 26, new Color(0.8f, 0.9f, 1f));
            cap.alignment = TextAnchor.MiddleLeft;
            Place(cap, new Vector2(0f, 0.5f), new Vector2(200, 50), new Vector2(20, 0));
            ((RectTransform)cap.transform).pivot = new Vector2(0f, 0.5f);
            ((RectTransform)cap.transform).anchorMin = ((RectTransform)cap.transform).anchorMax = new Vector2(0f, 0.5f);

            valueLabel = Label(name + "Value", row.transform, "0%", 24, new Color(0.7f, 0.82f, 0.95f));
            valueLabel.alignment = TextAnchor.MiddleRight;
            ((RectTransform)valueLabel.transform).anchorMin = ((RectTransform)valueLabel.transform).anchorMax = new Vector2(1f, 0.5f);
            ((RectTransform)valueLabel.transform).pivot = new Vector2(1f, 0.5f);
            ((RectTransform)valueLabel.transform).sizeDelta = new Vector2(90, 40);
            ((RectTransform)valueLabel.transform).anchoredPosition = new Vector2(-14, 0);

            var sliderGo = new GameObject(name + "Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var srt = (RectTransform)sliderGo.transform;
            srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.offsetMin = new Vector2(20, 4); srt.offsetMax = new Vector2(-20, 20);

            var bg = Img("Background", sliderGo.transform, null, new Color(0.1f, 0.14f, 0.22f, 1f), raycast: true);
            Stretch((RectTransform)bg.transform);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var far = (RectTransform)fillArea.transform;
            far.anchorMin = Vector2.zero; far.anchorMax = Vector2.one;
            far.offsetMin = new Vector2(0, 0); far.offsetMax = new Vector2(0, 0);

            var fill = Img("Fill", fillArea.transform, null, new Color(0.35f, 0.8f, 1f, 1f));
            var frt = (RectTransform)fill.transform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(1f, 1f);
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            var hrt = (RectTransform)handleArea.transform;
            hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
            hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;

            var handle = Img("Handle", handleArea.transform, null, new Color(0.85f, 0.95f, 1f, 1f), raycast: true);
            var handleRt = (RectTransform)handle.transform;
            handleRt.sizeDelta = new Vector2(16, 28);

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = frt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.75f;

            return slider;
        }

        private static Toggle ToggleRow(Transform parent, string name, string caption, float anchorY)
        {
            var row = new GameObject(name + "Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            Place(row.transform, new Vector2(0.5f, anchorY), new Vector2(600, 60), Vector2.zero);

            var cap = Label(name + "Caption", row.transform, caption, 26, new Color(0.8f, 0.9f, 1f));
            cap.alignment = TextAnchor.MiddleLeft;
            ((RectTransform)cap.transform).anchorMin = ((RectTransform)cap.transform).anchorMax = new Vector2(0f, 0.5f);
            ((RectTransform)cap.transform).pivot = new Vector2(0f, 0.5f);
            ((RectTransform)cap.transform).sizeDelta = new Vector2(260, 50);
            ((RectTransform)cap.transform).anchoredPosition = new Vector2(20, 0);

            var toggleGo = new GameObject(name + "Toggle", typeof(RectTransform), typeof(Toggle));
            toggleGo.transform.SetParent(row.transform, false);
            var trt = (RectTransform)toggleGo.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(1f, 0.5f);
            trt.pivot = new Vector2(1f, 0.5f);
            trt.sizeDelta = new Vector2(48, 48);
            trt.anchoredPosition = new Vector2(-16, 0);

            var bg = Img("Background", toggleGo.transform, null, new Color(0.1f, 0.14f, 0.22f, 1f), raycast: true);
            Stretch((RectTransform)bg.transform);
            var check = Img("Checkmark", bg.transform, null, new Color(0.4f, 0.9f, 1f, 1f));
            Place(check, new Vector2(0.5f, 0.5f), new Vector2(28, 28), Vector2.zero);

            var toggle = toggleGo.GetComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.graphic = check;
            toggle.isOn = true;
            return toggle;
        }

        // ---------------------------------------------------------------- primitives

        private static Canvas Canvas(string name, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
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
            t.raycastTarget = false;
            return t;
        }

        private static Button TextButton(string name, Transform parent, Sprite slab, string caption)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var im = go.GetComponent<Image>();
            im.sprite = slab;
            im.type = Image.Type.Sliced;
            var b = go.GetComponent<Button>();
            b.transition = Selectable.Transition.ColorTint;
            var cb = b.colors;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            cb.pressedColor = new Color(0.8f, 0.85f, 0.95f);
            b.colors = cb;
            var label = Label("Label", go.transform, caption, 34, new Color(0.94f, 0.98f, 1f));
            Stretch(label.rectTransform);
            return b;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void Reposition(Transform parent, string child, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            var t = parent.Find(child);
            if (t != null) Place(t, anchor, size, pos);
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

        private static void SetBorder(string sub, int l, int b, int r, int t)
        {
            string path = Ui + sub;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            var s = importer.spriteBorder;
            var want = new Vector4(l, b, r, t);
            if (s == want) return;
            importer.spriteBorder = want;
            importer.SaveAndReimport();
        }
    }
}
