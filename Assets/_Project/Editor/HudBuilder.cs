using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using SpaceSurvivors.Game;
using SpaceSurvivors.Progression;
using SpaceSurvivors.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// M10 wave 2 — rebuilds the in-run HUD and the level-up modal as real scene UI with the
    /// CraftPix kit, and wires the (now prefab-style) <see cref="RunHud"/> / <see cref="LevelUpScreen"/>
    /// components. Editor tool (RunCommand can't touch UnityEngine.UI). Idempotent.
    /// </summary>
    internal static class HudBuilder
    {
        private const string Ui = "Assets/_Project/Art/UI/PNG/";
        private static Font Legacy => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static Sprite S(string sub) => AssetDatabase.LoadAssetAtPath<Sprite>(Ui + sub);
        private static Sprite UiSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        [MenuItem("SpaceSurvivors/Build/M10 HUD + Level-Up (Game scene)")]
        private static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene().path == "Assets/_Project/Scenes/Game.unity"
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene("Assets/_Project/Scenes/Game.unity", OpenSceneMode.Single);

            GameObject ui = null;
            foreach (var g in scene.GetRootGameObjects()) if (g.name == "UI") ui = g;
            if (ui == null) { Debug.LogError("[HUD] UI root missing"); return; }

            Border("Loading_Bar/Table.png", 24, 22, 24, 22);
            Border("Loading_Bar/Loading_Bar_1_2.png", 18, 14, 18, 14);
            Border("Main_UI/Health_Bar_Table.png", 26, 26, 60, 26);
            Border("Main_UI/Armor_Bar_Table.png", 26, 26, 60, 26);
            Border("Level_Menu/Window.png", 34, 96, 34, 150);

            BakeVignette();
            BuildHud(ui);
            BuildLevelUp(ui);
            WireVignette(ui);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HUD] rebuilt HUD + level-up with CraftPix art.");
        }

        // ---------------------------------------------------------------- HUD

        private static void BuildHud(GameObject ui)
        {
            var prior = ui.transform.Find("HudCanvas");
            if (prior != null) Object.DestroyImmediate(prior.gameObject);

            var hud = ui.GetComponent<RunHud>() ?? ui.AddComponent<RunHud>();
            var canvas = Canvas("HudCanvas", ui.transform, 100);
            var root = canvas.transform;

            // --- ROW 0: XP bar across the very top, level number inside its left end.
            //     Same rounded "Loading_Bar/Table" frame + inset rounded fill as the HP bar,
            //     so the two bars read as one family.
            var xpBar = Bar("XpBar", root, out var xpFill, new Color(1f, 0.72f, 0.22f));
            var xpRt = xpBar.rectTransform;
            xpRt.anchorMin = new Vector2(0f, 1f); xpRt.anchorMax = new Vector2(1f, 1f);
            xpRt.pivot = new Vector2(0.5f, 1f);
            xpRt.offsetMin = new Vector2(28, -46); xpRt.offsetMax = new Vector2(-28, -8);

            var levelLabel = Label("Level", xpBar.transform, "LV 1", 22, new Color(1f, 0.97f, 0.85f));
            levelLabel.fontStyle = FontStyle.Bold;
            levelLabel.alignment = TextAnchor.MiddleLeft;
            var llRt = levelLabel.rectTransform;
            llRt.anchorMin = new Vector2(0f, 0f); llRt.anchorMax = new Vector2(0f, 1f);
            llRt.pivot = new Vector2(0f, 0.5f);
            llRt.sizeDelta = new Vector2(90, 0);
            llRt.anchoredPosition = new Vector2(20, 0);

            // --- ROW 1: timer (centre), health (left), scrap (right) — all clear of the XP bar
            var timerGroup = new GameObject("TimerGroup", typeof(RectTransform));
            timerGroup.transform.SetParent(root, false);
            Place(timerGroup.transform, new Vector2(0.5f, 1f), new Vector2(240, 52), new Vector2(-8, -80));
            var clock = Img("ClockIcon", timerGroup.transform, S("Main_UI/Clock_Icon.png"), new Color(0.8f, 0.92f, 1f));
            var clRt = clock.rectTransform;
            clRt.anchorMin = clRt.anchorMax = new Vector2(0f, 0.5f);
            clRt.pivot = new Vector2(0f, 0.5f);
            clRt.sizeDelta = new Vector2(34, 38);
            clRt.anchoredPosition = new Vector2(4, 0);
            var timer = Label("Timer", timerGroup.transform, "00:00", 32, new Color(0.92f, 0.97f, 1f));
            timer.alignment = TextAnchor.MiddleLeft;
            var tmRt = timer.rectTransform;
            tmRt.anchorMin = tmRt.anchorMax = new Vector2(0f, 0.5f);
            tmRt.pivot = new Vector2(0f, 0.5f);
            tmRt.sizeDelta = new Vector2(170, 48);
            tmRt.anchoredPosition = new Vector2(50, 0);

            // --- health bar (left) — matches the XP bar's frame + rounded fill
            var healthBar = Bar("HealthBar", root, out var hpFill, new Color(0.36f, 0.85f, 0.42f));
            Place(healthBar, new Vector2(0f, 1f), new Vector2(320, 40), new Vector2(190, -80));
            var hpText = Label("HpText", healthBar.transform, "100/100", 22, new Color(0.96f, 1f, 0.96f));
            hpText.fontStyle = FontStyle.Bold;
            Stretch(hpText.rectTransform);

            // --- shield, below health
            var shieldGroup = new GameObject("ShieldGroup", typeof(RectTransform));
            shieldGroup.transform.SetParent(root, false);
            Place(shieldGroup.transform, new Vector2(0f, 1f), new Vector2(260, 38), new Vector2(160, -124));
            var shieldBar = Img("ShieldBar", shieldGroup.transform, S("Loading_Bar/Table.png"), new Color(0.8f, 0.88f, 1f));
            shieldBar.type = Image.Type.Sliced;
            Stretch(shieldBar.rectTransform);
            var shieldText = Label("ShieldText", shieldGroup.transform, "", 22, new Color(0.62f, 0.9f, 1f));
            shieldText.alignment = TextAnchor.MiddleCenter;
            Stretch(shieldText.rectTransform);

            // --- scrap counter, top-right
            var scrapGroup = new GameObject("ScrapGroup", typeof(RectTransform));
            scrapGroup.transform.SetParent(root, false);
            Place(scrapGroup.transform, new Vector2(1f, 1f), new Vector2(180, 52), new Vector2(-40, -80));
            var crys = Img("Crystal", scrapGroup.transform, S("Main_UI/Cristal_Icon.png"), Color.white);
            Place(crys, new Vector2(1f, 0.5f), new Vector2(34, 48), new Vector2(0, 0));
            var scrapText = Label("Scrap", scrapGroup.transform, "0", 28, new Color(0.7f, 1f, 0.78f));
            scrapText.alignment = TextAnchor.MiddleRight;
            Place(scrapText, new Vector2(1f, 0.5f), new Vector2(120, 50), new Vector2(-46, 0));

            var so = new SerializedObject(hud);
            var ls = Object.FindFirstObjectByType<LevelSystem>();
            so.FindProperty("_levelSystem").objectReferenceValue = ls;
            so.FindProperty("_clock").objectReferenceValue = Object.FindFirstObjectByType<RunClock>();
            so.FindProperty("_playerHealth").objectReferenceValue = ls != null ? ls.GetComponent<HealthComponent>() : null;
            so.FindProperty("_shield").objectReferenceValue = Object.FindFirstObjectByType<ShieldComponent>();
            so.FindProperty("_scrap").objectReferenceValue = Object.FindFirstObjectByType<ScrapCollector>();
            so.FindProperty("_xpFill").objectReferenceValue = xpFill;
            so.FindProperty("_levelLabel").objectReferenceValue = levelLabel;
            so.FindProperty("_timerLabel").objectReferenceValue = timer;
            so.FindProperty("_healthFill").objectReferenceValue = hpFill;
            so.FindProperty("_healthLabel").objectReferenceValue = hpText;
            so.FindProperty("_shieldGroup").objectReferenceValue = shieldGroup;
            so.FindProperty("_shieldLabel").objectReferenceValue = shieldText;
            so.FindProperty("_scrapLabel").objectReferenceValue = scrapText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Level-up

        private static void BuildLevelUp(GameObject ui)
        {
            var prior = ui.transform.Find("LevelUpCanvas");
            if (prior != null) Object.DestroyImmediate(prior.gameObject);

            var screen = ui.GetComponent<LevelUpScreen>() ?? ui.AddComponent<LevelUpScreen>();
            var canvas = Canvas("LevelUpCanvas", ui.transform, 450);

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)panel.transform);

            var dim = Img("Dim", panel.transform, null, new Color(0.02f, 0.03f, 0.06f, 0.88f), raycast: true);
            Stretch(dim.rectTransform);

            var window = Img("Window", panel.transform, S("Level_Menu/Window.png"), Color.white);
            window.type = Image.Type.Sliced;
            Place(window, new Vector2(0.5f, 0.5f), new Vector2(780, 900), Vector2.zero);

            var header = Label("Header", window.transform, "LEVEL 1", 46, new Color(0.85f, 0.95f, 1f));
            header.fontStyle = FontStyle.Bold;
            Place(header, new Vector2(0.5f, 1f), new Vector2(560, 90), new Vector2(0, -46));

            var sub = Label("Sub", window.transform, "CHOOSE AN UPGRADE", 24, new Color(0.6f, 0.78f, 0.92f));
            Place(sub, new Vector2(0.5f, 1f), new Vector2(560, 40), new Vector2(0, -104));

            var buttons = new Button[3];
            var labels = new Text[3];
            Sprite slab = S("Shop/Prise_BTN_Table.png");
            for (int i = 0; i < 3; i++)
            {
                var b = new GameObject($"Choice{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                b.transform.SetParent(window.transform, false);
                var im = b.GetComponent<Image>();
                im.sprite = slab; im.type = Image.Type.Sliced; im.color = new Color(0.16f, 0.22f, 0.34f);
                var btn = b.GetComponent<Button>();
                btn.targetGraphic = im;
                btn.transition = Selectable.Transition.ColorTint;
                var cb = btn.colors; cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f); btn.colors = cb;
                Place(b.transform, new Vector2(0.5f, 0.62f - i * 0.20f), new Vector2(620, 150), Vector2.zero);

                var lab = Label("Label", b.transform, "", 30, Color.white);
                lab.supportRichText = true;
                Stretch(lab.rectTransform, 22f);
                buttons[i] = btn; labels[i] = lab;
            }

            panel.SetActive(false);

            var so = new SerializedObject(screen);
            so.FindProperty("_levelSystem").objectReferenceValue = Object.FindFirstObjectByType<LevelSystem>();
            so.FindProperty("_upgrades").objectReferenceValue = Object.FindFirstObjectByType<UpgradeService>();
            so.FindProperty("_run").objectReferenceValue = Object.FindFirstObjectByType<RunController>();
            so.FindProperty("_panel").objectReferenceValue = panel;
            so.FindProperty("_header").objectReferenceValue = header;
            var bp = so.FindProperty("_choiceButtons"); bp.arraySize = 3;
            var lp = so.FindProperty("_choiceLabels"); lp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                bp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
                lp.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- vignette

        private const string VignettePath = "Assets/_Project/Art/Sprites/Generated/DamageVignette.png";

        private static void BakeVignette()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath) != null) return;

            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            Vector2 c = new Vector2(size / 2f, size / 2f);
            float maxD = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxD;    // 0 centre, 1 corner-ish
                float a = Mathf.Clamp01(Mathf.SmoothStep(0f, 1f, (d - 0.45f) / 0.55f));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(VignettePath));
            System.IO.File.WriteAllBytes(VignettePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(VignettePath, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(VignettePath);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            Debug.Log("[HUD] baked " + VignettePath);
        }

        private static void WireVignette(GameObject ui)
        {
            var dv = ui.GetComponent<DamageVignette>();
            if (dv == null) return;
            var so = new SerializedObject(dv);
            so.FindProperty("_vignetteSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);
            so.ApplyModifiedPropertiesWithoutUndo();
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

        /// <summary>Rounded "Loading_Bar/Table" frame + an inset rounded horizontal fill.
        /// Used for both the XP bar and the health bar so they share a look.</summary>
        private static Image Bar(string name, Transform parent, out Image fill, Color fillColor)
        {
            var frame = Img(name, parent, S("Loading_Bar/Table.png"), new Color(0.8f, 0.88f, 1f));
            frame.type = Image.Type.Sliced;

            fill = Img("Fill", frame.transform, UiSprite(), fillColor);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            var r = fill.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(9, 8); r.offsetMax = new Vector2(-9, -8);
            return frame;
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
            t.font = Legacy; t.fontSize = size; t.color = color; t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static void Stretch(RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
        }

        private static void Place(Component c, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private static void Border(string sub, int l, int b, int r, int t)
        {
            var imp = AssetImporter.GetAtPath(Ui + sub) as TextureImporter;
            if (imp == null) return;
            var want = new Vector4(l, b, r, t);
            if (imp.spriteBorder == want) return;
            imp.spriteBorder = want;
            imp.SaveAndReimport();
        }
    }
}
