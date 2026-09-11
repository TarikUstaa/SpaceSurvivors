using System.Linq;
using SpaceSurvivors.Core;
using SpaceSurvivors.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// M20 — brings the Hangar / Achievements / Shop / Map-select screens and the main-menu
    /// settings panel in line with the redesigned main menu: the same live nebula + starfield
    /// backdrop behind the UI, Orbitron / Rajdhani fonts, the flat glass <c>MenuPanel</c> on
    /// windows and buttons, a clean title label in place of the CraftPix baked-text headers.
    /// A restyle pass — it never touches layout, wiring or component references. Idempotent.
    /// Menu: SpaceSurvivors/Build/M20 Skin meta screens.
    /// </summary>
    internal static class MetaScreenSkinner
    {
        private const string Gen = "Assets/_Project/Art/Sprites/Generated/";
        private const string Fonts = "Assets/_Project/Art/Fonts/";

        private static readonly Color Ink = new(0.82f, 0.97f, 1f);
        private static readonly Color GlassPanel = new(0.5f, 0.72f, 1f, 0.92f);
        private static readonly Color GlassButton = new(0.6f, 0.84f, 1f, 0.92f);
        private static readonly Color GlassInset = new(0.55f, 0.75f, 1f, 0.5f);

        [MenuItem("SpaceSurvivors/Build/M20 Skin meta screens")]
        private static void Build()
        {
            SkinScene("Assets/_Project/Scenes/Hangar.unity", "HANGAR", primaryY: 168f, backY: 70f);
            SkinScene("Assets/_Project/Scenes/Achievements.unity", "ACHIEVEMENTS", tightenGrid: true, panelHeight: 1000f);
            SkinScene("Assets/_Project/Scenes/Shop.unity", "UPGRADES");
            SkinScene("Assets/_Project/Scenes/MapSelect.unity", "SELECT MAP");
            SkinScene("Assets/_Project/Scenes/Profile.unity", "PROFILE");
            SkinSettingsPanel();
            Debug.Log("[MetaScreenSkinner] meta screens skinned.");
        }

        private static void SkinScene(string path, string title, bool tightenGrid = false,
                                      float panelHeight = 0f, float primaryY = 184f, float backY = 84f)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            var cam = Object.FindObjectsByType<Camera>()
                .FirstOrDefault(c => c.CompareTag("MainCamera")) ?? Camera.main;
            var scaler = Object.FindAnyObjectByType<CanvasScaler>();
            if (cam == null || scaler == null)
            {
                Debug.LogWarning($"[MetaScreenSkinner] {path}: camera or canvas missing, skipped.");
                return;
            }
            var canvas = scaler.transform;

            SetupCameraVolume(cam);
            AddBackdrop(cam);

            // The old full-screen CraftPix art — the diorama replaces it.
            Disable(canvas, "Background");
            Disable(canvas, "BG");

            AddVignette(canvas);

            // Window / content card
            var panel = FindDeep(canvas, "Panel") ?? FindDeep(canvas, "Window");
            if (panel != null && panel.TryGetComponent(out Image panelImg))
            {
                panelImg.sprite = Panel();
                panelImg.type = Image.Type.Sliced;
                panelImg.color = GlassPanel;

                if (panelHeight > 0f) // absolute → safe to re-run
                {
                    var prt = (RectTransform)panel;
                    prt.sizeDelta = new Vector2(prt.sizeDelta.x, panelHeight);
                }
            }

            // CraftPix baked-text header plates → a clean Orbitron title
            DisableComponent<Image>(canvas, "Header");
            DisableComponent<Text>(canvas, "HeaderText");
            AddTitle(panel != null ? panel : canvas, title);

            RefontAndReskin(canvas);
            CenterWalletRow(canvas);
            if (tightenGrid && panel != null) TightenTileGrid(panel);
            FixBottomButtons(panel != null ? panel : canvas, primaryY, backY);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SkinSettingsPanel()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var scaler = Object.FindAnyObjectByType<CanvasScaler>();
            if (scaler == null) return;
            var group = FindDeep(scaler.transform, "MenuSettingsGroup");
            if (group == null) return;

            // Fully opaque dim — it's a modal, the menu behind must not bleed through
            // (regression guard: QA already had to fix this once).
            if (FindDeep(group, "Dim") is { } dim && dim.TryGetComponent(out Image dimImg))
                dimImg.color = new Color(0.015f, 0.02f, 0.05f, 1f);

            var win = FindDeep(group, "Window");
            if (win != null && win.TryGetComponent(out Image winImg))
            {
                winImg.sprite = Panel();
                winImg.type = Image.Type.Sliced;
                winImg.color = GlassButton;
                var wrt = (RectTransform)win;
                wrt.anchorMin = wrt.anchorMax = wrt.pivot = new Vector2(0.5f, 0.5f);
                // 700, not 600: five rows at 96 apart plus the cloud-sync explanation and the
                // close button do not fit in 600 — the last row lands on top of the button.
                wrt.sizeDelta = new Vector2(660, 700);
                wrt.anchoredPosition = Vector2.zero;

                // opaque backing so the modal reads solid, not see-through (MenuPanel's own
                // texture alpha is low, and Image.color can't raise it)
                var backT = win.Find("WindowFill");
                if (backT == null)
                {
                    var b = new GameObject("WindowFill", typeof(RectTransform));
                    b.transform.SetParent(win, false);
                    b.transform.SetSiblingIndex(0);
                    var bi = b.AddComponent<Image>();
                    bi.color = new Color(0.06f, 0.09f, 0.16f, 0.98f);
                    bi.raycastTarget = false;
                    var brt = (RectTransform)b.transform;
                    brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                    brt.offsetMin = new Vector2(6, 6); brt.offsetMax = new Vector2(-6, -6);
                }
            }

            DisableComponent<Image>(group, "Header");
            AddTitle(win != null ? win : group, "SETTINGS");
            RefontAndReskin(group);
            LayoutSettingsRows(win);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Re-space the slider / toggle rows and the close button inside the window so
        /// nothing crowds the title or overlaps the frame (the original anchors were too tight).</summary>
        private static void LayoutSettingsRows(Transform win)
        {
            if (win == null) return;

            // This method, not the builder's anchors, decides where the rows sit — it runs last
            // and overwrites them. A row added to the panel and not added here keeps its
            // builder position and lands on top of whatever the skinner puts in that space.
            string[] rows = { "MasterRow", "MusicRow", "SfxRow", "FullscreenRow", "CloudSyncRow" };
            float y = -150f;
            foreach (var name in rows)
            {
                var r = FindDeep(win, name);
                if (r == null) continue;
                var rt = (RectTransform)r;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(540, 72);
                rt.anchoredPosition = new Vector2(0f, y);
                y -= 96f;

                // slider fill / groove / handle in the menu's ice-blue
                if (FindDeep(r, "Fill") is { } f && f.TryGetComponent(out Image fill))
                    fill.color = new Color(0.42f, 0.84f, 1f, 1f);
                foreach (var g in r.GetComponentsInChildren<Image>(true))
                    if (g.name == "Background")
                        g.color = new Color(0.09f, 0.13f, 0.22f, 1f);
                    else if (g.name == "Handle")
                        g.color = new Color(0.9f, 0.97f, 1f, 1f);
                    else if (g.name == "Checkmark")
                        g.color = new Color(0.42f, 0.9f, 1f, 1f);
            }

            // Sits directly under the cloud-sync row it explains, in the gap left by `y` having
            // already stepped past the last row.
            if (FindDeep(win, "CloudSyncStatus") is { } status)
            {
                var srt = (RectTransform)status;
                srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 1f);
                srt.sizeDelta = new Vector2(560, 30);
                srt.anchoredPosition = new Vector2(0f, y + 36f);
            }

            var close = FindDeep(win, "CloseButton");
            if (close != null)
            {
                var rt = (RectTransform)close;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(240, 62);
                rt.anchoredPosition = new Vector2(0f, 34f);
            }
        }

        // ------------------------------------------------------------------ backdrop

        private static void SetupCameraVolume(Camera cam)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f);
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var volGo = GameObject.Find("MenuVolume") ?? new GameObject("MenuVolume");
            var vol = volGo.GetComponent<Volume>() ?? volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Settings/PostProcess/GameVolume.asset");
        }

        private static void AddBackdrop(Camera cam)
        {
            var old = GameObject.Find("MenuBackdrop");
            if (old != null) Object.DestroyImmediate(old);

            var go = new GameObject("MenuBackdrop");
            var sf = go.AddComponent<StarfieldParallax>();
            var sfo = new SerializedObject(sf);
            sfo.FindProperty("_camera").objectReferenceValue = cam;
            sfo.FindProperty("_starSprite").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(Gen + "StarTile.png");
            sfo.FindProperty("_tint").colorValue = new Color(0.72f, 0.8f, 1f, 1f);
            sfo.FindProperty("_coverage").floatValue = 3f;
            sfo.FindProperty("_baseSortingOrder").intValue = -100;
            sfo.FindProperty("_backdropParallax").floatValue = 0.03f;
            sfo.FindProperty("_backdropDensity").floatValue = 1f;
            sfo.ApplyModifiedPropertiesWithoutUndo();

            var dio = cam.GetComponent<MenuDiorama>() ?? cam.gameObject.AddComponent<MenuDiorama>();
            var dso = new SerializedObject(dio);
            dso.FindProperty("_camera").objectReferenceValue = cam;
            dso.FindProperty("_starfield").objectReferenceValue = sf;
            dso.FindProperty("_fallbackNebula").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Sprites/Backgrounds/Blue_Nebula_05.png");
            // calmer drift on the deeper screens — the content, not the sky, is the focus
            dso.FindProperty("_driftAmplitude").vector2Value = new Vector2(0.9f, 0.5f);
            dso.FindProperty("_mouseLean").floatValue = 0.35f;
            dso.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddVignette(Transform canvas)
        {
            Disable(canvas, "Vignette");
            var t = canvas.Find("Vignette");
            if (t != null) Object.DestroyImmediate(t.gameObject);

            var go = new GameObject("Vignette", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var img = go.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Gen + "MenuVignette.png");
            img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.transform.SetSiblingIndex(0);
        }

        // ------------------------------------------------------------------ font + sprite pass

        private static void RefontAndReskin(Transform root)
        {
            Font orbitron = LoadFont("Orbitron/static/Orbitron-Bold.ttf");
            Font rajExtra = LoadFont("Rajdhani/Rajdhani-SemiBold.ttf");
            Font rajBody = LoadFont("Rajdhani/Rajdhani-Medium.ttf");

            foreach (var txt in root.GetComponentsInChildren<Text>(true))
            {
                if (txt.name == "SkinTitle") continue;
                bool big = txt.fontSize >= 34 || txt.name.Contains("Header") || txt.name == "Name";
                bool value = txt.name.Contains("Value") || txt.name.Contains("Cost") ||
                             txt.name.Contains("Level") || txt.name.Contains("Wallet") ||
                             txt.name.Contains("Price") || txt.name == "Chip";
                txt.font = big ? orbitron : value ? rajExtra : rajBody;

                // pull near-white text toward the menu's ice-blue
                var c = txt.color;
                if (c.r > 0.7f && c.g > 0.7f && c.b > 0.7f)
                    txt.color = new Color(Ink.r, Ink.g, Ink.b, c.a);
            }

            foreach (var btn in root.GetComponentsInChildren<Button>(true))
            {
                if (btn.name.Contains("Prev") || btn.name.Contains("Next") || btn.name.Contains("Arrow"))
                    continue; // keep the arrow glyph buttons
                if (!btn.TryGetComponent(out Image img)) continue;
                img.sprite = Panel();
                img.type = Image.Type.Sliced;
                img.color = GlassButton;

                var cb = btn.colors;
                cb.normalColor = GlassButton;
                cb.highlightedColor = new Color(0.82f, 0.94f, 1f, 1f);
                cb.pressedColor = new Color(0.5f, 0.7f, 0.85f, 1f);
                cb.fadeDuration = 0.08f;
                btn.colors = cb;
            }

            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                string n = img.name;
                bool inset = n.StartsWith("Tile_") || n.StartsWith("Row_") || n.EndsWith("Frame") ||
                             n.EndsWith("Table") || n == "PreviewFrame" || n == "Panel_Inner" ||
                             n == "StatsColumn" || n == "Stats";
                if (!inset) continue;
                img.sprite = Panel();
                img.type = Image.Type.Sliced;
                img.color = GlassInset;
            }
        }

        /// <summary>Pull the Achievements 2×4 tile grid up and tighten its row pitch so there's
        /// room for the Back button below it without the card overflowing the screen. Absolute
        /// positions (derived from each tile's column + row) → safe to re-run.</summary>
        private static void TightenTileGrid(Transform panel)
        {
            const float y0 = 150f, step = 150f, tileH = 140f;

            foreach (var img in panel.GetComponentsInChildren<Image>(true))
            {
                if (!img.name.StartsWith("Tile_")) continue;
                var rt = (RectTransform)img.transform;

                // recover row/col from the builder's original layout (x0 = -280, y0 = 208, step 180)
                int col = rt.anchoredPosition.x > 0f ? 1 : 0;
                int row = Mathf.Clamp(Mathf.RoundToInt((208f - rt.anchoredPosition.y) / 180f), 0, 3);

                rt.sizeDelta = new Vector2(rt.sizeDelta.x, tileH);
                rt.anchoredPosition = new Vector2(col == 0 ? -280f : 280f, y0 - row * step);
            }
        }

        /// <summary>The builders anchor the bottom Back / primary buttons so close to the
        /// panel edge that their rounded corners touch the frame. Lift them clear.
        /// <paramref name="primaryY"/> / <paramref name="backY"/> let a crowded screen (Hangar
        /// — three stat lines sit right above the primary button) push both buttons lower.</summary>
        private static void FixBottomButtons(Transform panel, float primaryY = 184f, float backY = 84f)
        {
            var primary = FindDeep(panel, "ActionButton") ?? FindDeep(panel, "PlayButton");
            if (primary != null)
            {
                var rt = (RectTransform)primary;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, primaryY);
            }

            var back = FindDeep(panel, "BackButton");
            if (back != null)
            {
                var rt = (RectTransform)back;
                rt.sizeDelta = new Vector2(280f, 68f);
                rt.anchoredPosition = new Vector2(0f, primary != null ? backY : 60f);
            }
        }

        /// <summary>Center the "SCRAP n" wallet chip directly under the screen title
        /// (Hangar / Shop). The builders right-align it, which reads as off-centre now that
        /// the CraftPix header plate is gone.</summary>
        private static void CenterWalletRow(Transform canvas)
        {
            var wg = FindDeep(canvas, "WalletGroup");
            if (wg == null) return;

            var rt = (RectTransform)wg;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(320, 40);
            rt.anchoredPosition = new Vector2(0f, -92f);

            var hlg = wg.GetComponent<HorizontalLayoutGroup>() ?? wg.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 8f;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;

            // HLG with childControlWidth reads each child's preferred width (Text reports its
            // own text width) and lays them out as one centred group.
            foreach (Transform child in wg)
            {
                var crt = (RectTransform)child;
                crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = Vector2.zero;

                if (child.TryGetComponent(out Text txt))
                    txt.alignment = TextAnchor.MiddleCenter;
                else if (child.TryGetComponent(out Image _))
                {
                    var le = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = 30f; le.preferredHeight = 30f;
                }
            }
        }

        // ------------------------------------------------------------------ helpers

        private static void AddTitle(Transform parent, string text)
        {
            var existing = parent.Find("SkinTitle");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject("SkinTitle", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = LoadFont("Orbitron/static/Orbitron-Black.ttf");
            t.text = text;
            t.fontSize = 40;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Ink;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            var o = go.AddComponent<Outline>();
            o.effectColor = new Color(0.03f, 0.09f, 0.16f, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(760, 70);
            rt.anchoredPosition = new Vector2(0f, -26f);
        }

        private static Sprite Panel() => AssetDatabase.LoadAssetAtPath<Sprite>(Gen + "MenuPanel.png");

        private static Font LoadFont(string sub)
        {
            var f = AssetDatabase.LoadAssetAtPath<Font>(Fonts + sub);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                var hit = FindDeep(c, name);
                if (hit != null) return hit;
            }
            return null;
        }

        private static void Disable(Transform canvas, string name)
        {
            var t = canvas.Find(name);
            if (t != null) t.gameObject.SetActive(false);
        }

        private static void DisableComponent<T>(Transform root, string name) where T : Component
        {
            foreach (var c in root.GetComponentsInChildren<T>(true))
                if (c.name == name) c.gameObject.SetActive(false);
        }
    }
}
