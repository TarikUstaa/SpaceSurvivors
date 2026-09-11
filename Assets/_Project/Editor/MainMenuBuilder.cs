using System.IO;
using System.Linq;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
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
    /// M20 — "Plan A" main-menu redesign. Restyles + augments the existing MainMenu scene rather
    /// than rebuilding it (the settings panel / PanelToggle / meta buttons stay wired):
    ///   * a live diorama behind the UI — parallax starfield + the selected map's nebula + the
    ///     selected ship idling + drifting asteroids, lit by the shared Bloom volume;
    ///   * a slow camera drift that leans toward the mouse (<see cref="MenuDiorama"/>);
    ///   * Orbitron / Rajdhani fonts on every label, a glowing logo, glass button panels;
    ///   * a "pilot record" panel (best time / kills / scrap / achievements).
    /// Idempotent — safe to re-run. Menu: SpaceSurvivors/Build/M20 Main Menu redesign.
    /// </summary>
    internal static class MainMenuBuilder
    {
        private const string Scene = "Assets/_Project/Scenes/MainMenu.unity";
        private const string Gen = "Assets/_Project/Art/Sprites/Generated/";
        private const string Fonts = "Assets/_Project/Art/Fonts/";
        private const string Meteors = "Assets/_Project/Art/Sprites/Base_Assets/Meteors/";

        // palette
        private static readonly Color Ink = new(0.82f, 0.97f, 1f);
        private static readonly Color InkDim = new(0.66f, 0.82f, 0.95f);
        private static readonly Color InkMute = new(0.5f, 0.62f, 0.74f);
        private static readonly Color Panel = new(0.66f, 0.86f, 1f, 1f);

        /// <summary>
        /// The bottom meta row, left to right. Each button object is created by its own
        /// builder's menu item (Shop/Hangar/Profile/Achievements) or lives in the base scene
        /// (Settings); this list is only which ones to rescue and restyle here.
        /// </summary>
        private static readonly string[] MetaButtonNames =
            { "ShopButton", "HangarButton", "ProfileButton", "AchievementsButton", "SettingsButton" };

        // ------------------------------------------------------------------ entry

        [MenuItem("SpaceSurvivors/Build/M20 Main Menu redesign")]
        private static void Build()
        {
            BakePanel();
            BakeVignette();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            var cam = Object.FindObjectsByType<Camera>()
                .FirstOrDefault(c => c.CompareTag("MainCamera")) ?? Camera.main;
            if (cam == null) { Debug.LogError("[MainMenuBuilder] no Main Camera in the scene."); return; }

            SetupCamera(cam);
            var starfield = BuildWorld(cam);
            RestyleCanvas(cam, starfield);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MainMenuBuilder] M20 main menu built.");
        }

        // ------------------------------------------------------------------ camera + volume

        private static void SetupCamera(Camera cam)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f);

            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;

            var volGo = GameObject.Find("MenuVolume");
            if (volGo == null) volGo = new GameObject("MenuVolume");
            var vol = volGo.GetComponent<Volume>() ?? volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Settings/PostProcess/GameVolume.asset");
        }

        // ------------------------------------------------------------------ world diorama

        private static StarfieldParallax BuildWorld(Camera cam)
        {
            var world = GameObject.Find("MenuWorld");
            if (world != null) Object.DestroyImmediate(world);
            world = new GameObject("MenuWorld");

            // Starfield
            var sfGo = new GameObject("MenuStarfield");
            sfGo.transform.SetParent(world.transform, false);
            var sf = sfGo.AddComponent<StarfieldParallax>();
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

            // Showcase — ship + engine glow + drifting rocks.
            // z = 6 puts it in front of the orthographic near-clip plane (the camera sits at
            // z = 0, so anything at z = 0 is culled) but behind the nebula backdrop at z = 30.
            var showGo = new GameObject("MenuShowcase");
            showGo.transform.SetParent(world.transform, false);
            showGo.transform.localPosition = new Vector3(0f, 0f, 6f);
            var show = showGo.AddComponent<MenuShowcase>();

            var starterShip = AssetDatabase.LoadAssetAtPath<ShipData>("Assets/_Project/Resources/Ship_starter.asset");
            Sprite shipSprite = starterShip != null ? starterShip.sprite
                : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Sprites/Base_Assets/playerShip1_blue.png");

            // Keep the diorama clear of the UI: ship sits in the upper-right, well away from the
            // centred button column, the left pilot-record card and the bottom meta row.
            var ship = new GameObject("Ship");
            ship.transform.SetParent(showGo.transform, false);
            ship.transform.localPosition = new Vector3(8.0f, -1.3f, 0f);
            ship.transform.localScale = Vector3.one * 1.55f;
            var shipSr = ship.AddComponent<SpriteRenderer>();
            shipSr.sprite = shipSprite;
            shipSr.sortingOrder = 12;

            var glow = new GameObject("EngineGlow");
            glow.transform.SetParent(ship.transform, false);
            glow.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            glow.transform.localScale = Vector3.one * 0.95f;
            var glowSr = glow.AddComponent<SpriteRenderer>();
            glowSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Gen + "WeaponOrbGlow.png");
            glowSr.color = new Color(0.4f, 0.85f, 1f, 0.32f);
            glowSr.sortingOrder = 11;

            string[] rockArt =
            {
                "meteorGrey_big1.png", "meteorBrown_big3.png", "meteorGrey_med2.png",
                "meteorBrown_med1.png", "meteorGrey_small1.png",
            };
            // Scattered toward the frame edges — never the centre where the buttons live.
            Vector3[] rockPos =
            {
                new(-8.4f, -3.6f, 0f), new(8.6f, -4.2f, 0f), new(-8.8f, 3.4f, 0f),
                new(-5.5f, 6.4f, 0f), new(6.4f, 5.6f, 0f),
            };
            float[] rockScale = { 0.9f, 0.7f, 0.55f, 0.45f, 0.36f };
            var rocks = new SpriteRenderer[rockArt.Length];
            for (int i = 0; i < rockArt.Length; i++)
            {
                var r = new GameObject("Rock" + i);
                r.transform.SetParent(showGo.transform, false);
                r.transform.localPosition = rockPos[i];
                r.transform.localScale = Vector3.one * rockScale[i];
                var sr = r.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Meteors + rockArt[i]);
                sr.color = new Color(0.78f, 0.82f, 0.9f);
                sr.sortingOrder = 5;
                rocks[i] = sr;
            }

            var sho = new SerializedObject(show);
            sho.FindProperty("_ship").objectReferenceValue = shipSr;
            sho.FindProperty("_shipEngineGlow").objectReferenceValue = glow.transform;
            var rp = sho.FindProperty("_rocks");
            rp.arraySize = rocks.Length;
            for (int i = 0; i < rocks.Length; i++) rp.GetArrayElementAtIndex(i).objectReferenceValue = rocks[i];
            sho.ApplyModifiedPropertiesWithoutUndo();

            // Diorama driver on the camera
            var dio = cam.GetComponent<MenuDiorama>() ?? cam.gameObject.AddComponent<MenuDiorama>();
            var dio_so = new SerializedObject(dio);
            dio_so.FindProperty("_camera").objectReferenceValue = cam;
            dio_so.FindProperty("_starfield").objectReferenceValue = sf;
            dio_so.FindProperty("_fallbackNebula").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Sprites/Backgrounds/Blue_Nebula_05.png");
            dio_so.ApplyModifiedPropertiesWithoutUndo();

            return sf;
        }

        // ------------------------------------------------------------------ canvas restyle

        private static void RestyleCanvas(Camera cam, StarfieldParallax starfield)
        {
            var scaler = Object.FindAnyObjectByType<CanvasScaler>();
            if (scaler == null) { Debug.LogError("[MainMenuBuilder] MenuCanvas not found."); return; }
            var canvas = scaler.transform;
            var menuRoot = canvas.parent;

            Font semi = LoadFont(Fonts + "Rajdhani/Rajdhani-SemiBold.ttf");
            Font med = LoadFont(Fonts + "Rajdhani/Rajdhani-Medium.ttf");
            Font light = LoadFont(Fonts + "Rajdhani/Rajdhani-Light.ttf");

            var panelSpr = AssetDatabase.LoadAssetAtPath<Sprite>(Gen + "MenuPanel.png");
            var vignetteSpr = AssetDatabase.LoadAssetAtPath<Sprite>(Gen + "MenuVignette.png");

            // Rescue the meta buttons before nuking a MetaRow left by a previous run —
            // they carry LoadSceneButton / PanelToggle wiring we must not destroy.
            var oldRow = canvas.Find("MetaRow");
            if (oldRow != null)
                foreach (var n in MetaButtonNames)
                {
                    var b = oldRow.Find(n);
                    if (b != null) b.SetParent(canvas, false);
                }

            Kill(canvas, "BG");
            Kill(canvas, "Subtitle");
            Kill(canvas, "WalletLabel");
            Kill(canvas, "WalletChip");
            Kill(canvas, "Wallet");    // rebuilt below as WalletGroup, top-right
            Kill(canvas, "Vignette");
            Kill(canvas, "TitleRule");
            Kill(canvas, "Tagline");
            Kill(canvas, "TitleTop");
            Kill(canvas, "TitleShadow");
            Kill(canvas, "PilotRecord");       // replaced by the leaderboard; kill any left by an older run
            Kill(canvas, "MenuLeaderboard");
            Kill(canvas, "WalletGroup");
            Kill(canvas, "MetaRow");
            Kill(canvas, "VersionLabel");

            // Vignette — sits behind everything, keeps text legible over the starfield.
            var vig = NewImage("Vignette", canvas, vignetteSpr, Color.white);
            Stretch(vig.rectTransform);
            vig.raycastTarget = false;
            vig.transform.SetSiblingIndex(0);

            // Logo — a two-line lock-up: a small wide "SPACE" over a big "SURVIVORS", both
            // Audiowide. Chunky shadow + the global Bloom give it depth. (Legacy Text has no
            // letter-spacing, so the top line is spaced out by hand.)
            Font logo = LoadFont(Fonts + "Audiowide/Audiowide-Regular.ttf");

            var titleTop = NewText("TitleTop", canvas, logo, "S P A C E", 40, InkDim);
            AddOutline(titleTop.gameObject, new Color(0.03f, 0.09f, 0.16f, 0.9f), new Vector2(2f, -2f));
            Place((RectTransform)titleTop.transform, new Vector2(0.5f, 0.88f), new Vector2(1200, 60));

            // A dark duplicate one step down-right = a solid drop shadow (idempotent — its own
            // object, rebuilt each run).
            var shadow = NewText("TitleShadow", canvas, logo, "SURVIVORS", 116, new Color(0.02f, 0.05f, 0.1f, 0.85f));
            Place((RectTransform)shadow.transform, new Vector2(0.5f, 0.78f), new Vector2(1900, 190), new Vector2(6f, -8f));
            shadow.raycastTarget = false;

            var title = canvas.Find("Title").GetComponent<Text>();
            title.font = logo;
            title.text = "SURVIVORS";
            title.fontSize = 116;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Ink;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            AddOutline(title.gameObject, new Color(0.03f, 0.09f, 0.16f, 0.95f), new Vector2(3f, -3f));
            Place((RectTransform)title.transform, new Vector2(0.5f, 0.78f), new Vector2(1900, 190));
            title.transform.SetSiblingIndex(shadow.transform.GetSiblingIndex() + 1); // in front of its shadow

            // Primary buttons
            StylePrimaryButton(canvas, "CampaignButton", "CAMPAIGN", semi, panelSpr, new Vector2(0.5f, 0.35f));
            StylePrimaryButton(canvas, "InfiniteButton", "INFINITE", semi, panelSpr, new Vector2(0.5f, 0.235f));

            var desc = canvas.Find("Description").GetComponent<Text>();
            desc.font = med;
            desc.fontSize = 22;
            desc.color = InkDim;
            desc.alignment = TextAnchor.MiddleCenter;
            Place((RectTransform)desc.transform, new Vector2(0.5f, 0.145f), new Vector2(1100, 50));

            // Meta button row along the bottom
            var row = new GameObject("MetaRow", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(canvas, false);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.anchoredPosition = new Vector2(0f, 34f);
            row.sizeDelta = new Vector2(1000, 60);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;

            // ProfileButton is created by ProfileBuilder's own menu item; StyleMetaButton skips it
            // if it is not there yet, so the order the two builders run in does not matter.
            foreach (var name in MetaButtonNames)
                StyleMetaButton(canvas, name, row, semi, panelSpr);

            // Quit — small, bottom-right
            var quit = canvas.Find("QuitButton").GetComponent<Button>();
            var quitImg = quit.GetComponent<Image>();
            quitImg.sprite = panelSpr;
            quitImg.type = Image.Type.Sliced;
            quitImg.color = new Color(1f, 0.55f, 0.55f, 0.9f);
            Place((RectTransform)quit.transform, new Vector2(1f, 0f), new Vector2(120, 46), new Vector2(-24, 24));
            ((RectTransform)quit.transform).pivot = new Vector2(1f, 0f);
            foreach (Transform c in quit.transform) Object.DestroyImmediate(c.gameObject);
            var quitLabel = NewText("Label", quit.transform, semi, "QUIT", 20, new Color(1f, 0.9f, 0.9f));
            Stretch(quitLabel.rectTransform);

            // Leaderboard panel — left side, in the slot the pilot-record card used to hold.
            // The career stats it carried now live on their own Stats screen (StatsBuilder).
            BuildLeaderboard(canvas, panelSpr, semi, med);

            var version = NewText("VersionLabel", canvas, light, "v0.20", 16,
                new Color(InkMute.r, InkMute.g, InkMute.b, 0.7f));
            var vrt = (RectTransform)version.transform;
            vrt.anchorMin = vrt.anchorMax = vrt.pivot = new Vector2(0f, 0f);
            vrt.anchoredPosition = new Vector2(22f, 16f);
            vrt.sizeDelta = new Vector2(160, 24);
            version.alignment = TextAnchor.LowerLeft;

            // Settings window text
            RestyleSettings(canvas, semi, med);

            // Scrap purse — top-right, the one place the menu shows the wallet now that the
            // pilot-record card (which used to carry it) has become the leaderboard.
            var walletLabel = BuildWalletChip(canvas, semi);

            var menuScreen = menuRoot.GetComponent<SpaceSurvivors.UI.MainMenuScreen>();
            if (menuScreen != null)
            {
                var mso = new SerializedObject(menuScreen);
                mso.FindProperty("_walletLabel").objectReferenceValue = walletLabel;
                mso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void StylePrimaryButton(Transform canvas, string name, string label, Font font,
            Sprite panel, Vector2 anchor)
        {
            var btn = canvas.Find(name).GetComponent<Button>();
            var img = btn.GetComponent<Image>();
            img.sprite = panel;
            img.type = Image.Type.Sliced;
            img.color = Panel;
            Place((RectTransform)btn.transform, anchor, new Vector2(540, 104));

            var txt = btn.GetComponentInChildren<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 36;
            txt.color = new Color(0.94f, 0.99f, 1f);
            txt.alignment = TextAnchor.MiddleCenter;

            var colors = btn.colors;
            colors.normalColor = Panel;
            colors.highlightedColor = new Color(0.8f, 0.94f, 1f, 1f);
            colors.pressedColor = new Color(0.5f, 0.7f, 0.85f, 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
        }

        private static void StyleMetaButton(Transform canvas, string name, RectTransform row, Font font, Sprite panel)
        {
            var t = canvas.Find(name);
            if (t == null) return;
            var btn = t.GetComponent<Button>();
            t.SetParent(row, false);

            var le = t.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 200f;
            le.preferredHeight = 56f;

            var img = btn.GetComponent<Image>();
            img.sprite = panel;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.6f, 0.84f, 1f, 0.92f);

            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.font = font;
                txt.fontSize = 20;
                txt.color = new Color(0.86f, 0.95f, 1f);
                txt.alignment = TextAnchor.MiddleCenter;
                Stretch(txt.rectTransform);
            }
        }

        /// <summary>
        /// The ranked board in the menu's left slot: a title, two mode tabs, and a column of
        /// fixed rows filled at runtime by <see cref="MenuLeaderboard"/>. Taller and a little
        /// wider than the old pilot card because a list needs the room.
        /// </summary>
        /// <summary>
        /// The scrap purse in the top-right corner: chip icon + amount on a small panel.
        /// Returns the <see cref="Text"/> so the caller can hand it to
        /// <c>MainMenuScreen._walletLabel</c> — the label is only ever written by
        /// <c>RefreshWallet</c>, so an unwired field means the menu silently stops showing scrap.
        /// </summary>
        private static Text BuildWalletChip(Transform canvas, Font semi)
        {
            var group = NewImage("WalletGroup", canvas,
                AssetDatabase.LoadAssetAtPath<Sprite>(Gen + "MenuPanel.png"),
                new Color(0.55f, 0.78f, 1f, 0.85f));
            group.type = Image.Type.Sliced;
            group.raycastTarget = false;
            var rt = group.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(230, 52);
            rt.anchoredPosition = new Vector2(-24f, -20f);

            var chip = NewImage("Chip", group.transform,
                AssetDatabase.LoadAssetAtPath<Sprite>(Gen + "ScrapChip.png"), Color.white);
            chip.raycastTarget = false;
            var crt = chip.rectTransform;
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0f, 0.5f);
            crt.sizeDelta = new Vector2(30, 30);
            crt.anchoredPosition = new Vector2(16f, 0f);

            // Placeholder text only — MainMenuScreen.RefreshWallet overwrites it on enable.
            var label = NewText("WalletLabel", group.transform, semi, "SCRAP  0", 24, Ink);
            label.alignment = TextAnchor.MiddleRight;
            label.raycastTarget = false;
            var lrt = label.rectTransform;
            lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(1f, 0.5f);
            lrt.sizeDelta = new Vector2(170, 40);
            lrt.anchoredPosition = new Vector2(-16f, 0f);
            AddOutline(label.gameObject, new Color(0.03f, 0.09f, 0.16f, 0.9f), new Vector2(1.5f, -1.5f));

            return label;
        }

        private static void BuildLeaderboard(Transform canvas, Sprite panel, Font semi, Font med)
        {
            const int rowCount = 8;
            const float rowHeight = 34f;
            const float headerSpace = 96f;   // title + tab strip
            const float footerSpace = 30f;   // status line

            var card = NewImage("MenuLeaderboard", canvas, panel, new Color(0.55f, 0.78f, 1f, 0.92f));
            card.type = Image.Type.Sliced;
            var rt = card.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(40f, -20f);
            rt.sizeDelta = new Vector2(340, headerSpace + rowCount * rowHeight + footerSpace + 24f);

            var header = NewText("Header", card.transform, semi, "LEADERBOARD", 20, new Color(0.8f, 0.93f, 1f));
            var hrt = (RectTransform)header.transform;
            hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f); hrt.pivot = new Vector2(0.5f, 1f);
            hrt.sizeDelta = new Vector2(-32, 34); hrt.anchoredPosition = new Vector2(0f, -12f);
            header.alignment = TextAnchor.MiddleLeft;

            // Two tabs, side by side under the title.
            Button infiniteTab = Tab(card.transform, panel, semi, "INFINITE", 0f);
            Button campaignTab = Tab(card.transform, panel, semi, "CAMPAIGN", 0.5f);

            var board = card.gameObject.AddComponent<MenuLeaderboard>();
            var so = new SerializedObject(board);
            so.FindProperty("_infiniteTab").objectReferenceValue = infiniteTab;
            so.FindProperty("_campaignTab").objectReferenceValue = campaignTab;

            var rowsProp = so.FindProperty("_rows");
            rowsProp.arraySize = rowCount;
            for (int i = 0; i < rowCount; i++)
            {
                float top = -(headerSpace + i * rowHeight);
                var element = rowsProp.GetArrayElementAtIndex(i);
                BoardRow(card.transform, med, semi, i, top, rowHeight,
                    out var rowRoot, out var rank, out var name, out var time, out var highlight);
                element.FindPropertyRelative("root").objectReferenceValue = rowRoot;
                element.FindPropertyRelative("rank").objectReferenceValue = rank;
                element.FindPropertyRelative("name").objectReferenceValue = name;
                element.FindPropertyRelative("time").objectReferenceValue = time;
                element.FindPropertyRelative("highlight").objectReferenceValue = highlight;
            }

            var status = NewText("Status", card.transform, med, "", 15, new Color(0.62f, 0.76f, 0.9f));
            var srt = (RectTransform)status.transform;
            srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 0f); srt.pivot = new Vector2(0.5f, 0f);
            srt.sizeDelta = new Vector2(-28, 24); srt.anchoredPosition = new Vector2(0f, 10f);
            status.alignment = TextAnchor.MiddleLeft;
            status.fontStyle = FontStyle.Italic;
            so.FindProperty("_statusLabel").objectReferenceValue = status;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button Tab(Transform card, Sprite panel, Font font, string label, float xFraction)
        {
            var go = new GameObject(label + "Tab", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(card, false);
            var img = go.GetComponent<Image>();
            img.sprite = panel;
            img.type = Image.Type.Sliced;

            var brt = (RectTransform)go.transform;
            brt.anchorMin = new Vector2(xFraction, 1f);
            brt.anchorMax = new Vector2(xFraction + 0.5f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.offsetMin = new Vector2(xFraction == 0f ? 14f : 4f, 0f);
            brt.offsetMax = new Vector2(xFraction == 0f ? -4f : -14f, 0f);
            brt.sizeDelta = new Vector2(brt.sizeDelta.x, 32f);
            brt.anchoredPosition = new Vector2(brt.anchoredPosition.x, -52f);

            var txt = NewText("Label", go.transform, font, label, 15, new Color(0.9f, 0.97f, 1f));
            Stretch(txt.rectTransform);
            txt.alignment = TextAnchor.MiddleCenter;
            return go.GetComponent<Button>();
        }

        private static void BoardRow(Transform card, Font labelFont, Font valueFont, int index,
            float top, float height,
            out GameObject root, out Text rank, out Text name, out Text time, out Image highlight)
        {
            var rowGo = new GameObject($"Row{index}", typeof(RectTransform));
            rowGo.transform.SetParent(card, false);
            var rrt = (RectTransform)rowGo.transform;
            rrt.anchorMin = new Vector2(0f, 1f); rrt.anchorMax = new Vector2(1f, 1f); rrt.pivot = new Vector2(0.5f, 1f);
            rrt.sizeDelta = new Vector2(-20, height);
            rrt.anchoredPosition = new Vector2(0f, top);
            root = rowGo;

            var hi = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
            hi.transform.SetParent(rowGo.transform, false);
            var hiImg = hi.GetComponent<Image>();
            hiImg.color = new Color(1f, 0.86f, 0.4f, 0.16f);
            hiImg.raycastTarget = false;
            hiImg.enabled = false;
            Stretch((RectTransform)hi.transform);
            highlight = hiImg;

            rank = NewText("Rank", rowGo.transform, valueFont, "", 16, new Color(0.7f, 0.83f, 0.95f));
            var rankRt = (RectTransform)rank.transform;
            rankRt.anchorMin = new Vector2(0f, 0f); rankRt.anchorMax = new Vector2(0f, 1f); rankRt.pivot = new Vector2(0f, 0.5f);
            rankRt.sizeDelta = new Vector2(38f, 0f); rankRt.anchoredPosition = new Vector2(12f, 0f);
            rank.alignment = TextAnchor.MiddleLeft;

            name = NewText("Name", rowGo.transform, labelFont, "", 16, new Color(0.92f, 0.98f, 1f));
            var nameRt = (RectTransform)name.transform;
            nameRt.anchorMin = new Vector2(0f, 0f); nameRt.anchorMax = new Vector2(1f, 1f); nameRt.pivot = new Vector2(0f, 0.5f);
            nameRt.offsetMin = new Vector2(54f, 0f); nameRt.offsetMax = new Vector2(-78f, 0f);
            name.alignment = TextAnchor.MiddleLeft;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            time = NewText("Time", rowGo.transform, valueFont, "", 16, new Color(0.94f, 0.99f, 1f));
            var timeRt = (RectTransform)time.transform;
            timeRt.anchorMin = new Vector2(1f, 0f); timeRt.anchorMax = new Vector2(1f, 1f); timeRt.pivot = new Vector2(1f, 0.5f);
            timeRt.sizeDelta = new Vector2(74f, 0f); timeRt.anchoredPosition = new Vector2(-12f, 0f);
            time.alignment = TextAnchor.MiddleRight;
        }

        private static void RestyleSettings(Transform canvas, Font semi, Font med)
        {
            var group = canvas.Find("MenuSettingsGroup");
            if (group == null) return;
            foreach (var txt in group.GetComponentsInChildren<Text>(true))
                txt.font = txt.fontSize >= 30 ? semi : med;
        }

        // ------------------------------------------------------------------ sprite baking

        private static void BakePanel()
        {
            const int W = 96, H = 64, r = 16;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float inset = RoundedMask(x, y, W, H, r);
                if (inset <= 0f) { px[y * W + x] = new Color(0, 0, 0, 0); continue; }

                // dark translucent body, a bright 1px rim, a soft glow along the top edge
                float rim = 1f - Mathf.Clamp01((inset - 1f) / 2f);
                float top = Mathf.Clamp01((y - (H - 14)) / 14f);
                float a = Mathf.Lerp(0.28f, 0.9f, rim);
                a = Mathf.Max(a, 0.28f + top * 0.35f);
                float lum = Mathf.Lerp(0.14f, 0.85f, Mathf.Max(rim, top * 0.7f));
                px[y * W + x] = new Color(lum * 0.55f, lum * 0.82f, lum, a);
            }
            WriteSprite(tex, px, Gen + "MenuPanel.png", ppu: 100f, border: new Vector4(r, r, r, r));
        }

        private static void BakeVignette()
        {
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var px = new Color[S * S];
            Vector2 c = new(S / 2f, S / 2f);
            float maxD = c.magnitude;
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxD;   // 0 centre → 1 corner
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.42f) / 0.58f)) * 0.72f;
                a = Mathf.Max(a, 0.12f); // a gentle overall dim so bright stars don't fight the text
                px[y * S + x] = new Color(0.015f, 0.02f, 0.05f, a);
            }
            WriteSprite(tex, px, Gen + "MenuVignette.png", ppu: 100f, border: Vector4.zero);
        }

        private static float RoundedMask(int x, int y, int w, int h, int r)
        {
            float dx = Mathf.Min(x, w - 1 - x);
            float dy = Mathf.Min(y, h - 1 - y);
            if (dx >= r || dy >= r) return Mathf.Min(dx, dy) + 1f;
            float cx = dx < r ? r - dx : 0f;
            float cy = dy < r ? r - dy : 0f;
            float dist = Mathf.Sqrt(cx * cx + cy * cy);
            return r - dist;
        }

        private static void WriteSprite(Texture2D tex, Color[] px, string path, float ppu, Vector4 border)
        {
            tex.SetPixels(px);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.spritePixelsPerUnit = ppu;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            if (border != Vector4.zero)
            {
                var st = new TextureImporterSettings();
                imp.ReadTextureSettings(st);
                st.spriteBorder = border;
                st.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(st);
            }
            imp.SaveAndReimport();
        }

        // ------------------------------------------------------------------ tiny UI helpers

        private static Font LoadFont(string path)
        {
            var f = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (f == null) Debug.LogWarning("[MainMenuBuilder] missing font: " + path);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void Kill(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        private static Text NewText(string name, Transform parent, Font font, string text, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            var o = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = distance;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset = default)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }
    }
}
