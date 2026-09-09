using System.Collections.Generic;
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
    /// Builds <c>Assets/_Project/Scenes/Profile.unity</c> — the player's own page: the
    /// account at the top (rename, member since, country), the career numbers below,
    /// one scroll.
    ///
    /// <para>The content is a <see cref="VerticalLayoutGroup"/> under a
    /// <see cref="ScrollRect"/> with a <see cref="ContentSizeFitter"/>, so rows can be
    /// added or removed here without anyone recomputing a height by hand — the list grows
    /// and the scroll range follows.</para>
    ///
    /// <para>Editor tool because the runtime assemblies can't reference UnityEngine.UI.
    /// Idempotent: recreates the scene each run and re-registers it in Build Settings.
    /// Two menu items, as every meta screen has — one for the scene, one for the nav
    /// button. Re-run the M20 redesign afterwards to style the button.</para>
    /// </summary>
    internal static class ProfileBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Profile.unity";
        private const string OldStatsScene = "Assets/_Project/Scenes/Stats.unity";
        private const string Gen = "Assets/_Project/Art/Sprites/Generated/";

        private static Font Legacy => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static Sprite Panel => AssetDatabase.LoadAssetAtPath<Sprite>(Gen + "MenuPanel.png");

        private static readonly Color Ink = new(0.90f, 0.96f, 1f);
        private static readonly Color InkDim = new(0.64f, 0.78f, 0.92f);
        private static readonly Color Heading = new(0.75f, 0.90f, 1f);
        private static readonly Color Faint = new(0.55f, 0.68f, 0.82f);

        private const float RowHeight = 46f;
        private const float HeadHeight = 52f;

        // Career lines: label, the ProfileScreen field to wire, section header before it.
        private static readonly (string label, string field, string section)[] StatLines =
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

        [MenuItem("SpaceSurvivors/Build/Profile scene")]
        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
            cam.orthographic = true;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("ProfileCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = Img("Background", canvasGo.transform, null, new Color(0.05f, 0.06f, 0.10f), raycast: true);
            Stretch(bg.rectTransform);

            var screen = canvasGo.AddComponent<ProfileScreen>();
            var so = new SerializedObject(screen);

            // ---- window (MetaScreenSkinner finds it by name and puts the title inside) ----
            var window = Img("Window", canvasGo.transform, Panel, new Color(0.16f, 0.22f, 0.32f, 0.95f));
            window.type = Image.Type.Sliced;
            var wrt = window.rectTransform;
            wrt.anchorMin = wrt.anchorMax = wrt.pivot = new Vector2(0.5f, 0.5f);
            wrt.sizeDelta = new Vector2(820, 800);
            wrt.anchoredPosition = new Vector2(0f, 30f);

            // ---- scroll ----
            var content = BuildScroll(window.transform, out ScrollRect scroll);

            // ---- account section ----
            MajorHead(content, "ACCOUNT");

            BuildNameRow(content, so);

            // The player id is deliberately not shown. The server will hand it over — it is
            // theirs — but a raw uuid in a row is a wall of hex that means nothing to anyone
            // reading their own profile. It goes back in the day there is somewhere to quote
            // it to, next to something explaining why.
            so.FindProperty("_memberSinceValue").objectReferenceValue =
                ValueRow(content, "Member since", 22, Ink);
            so.FindProperty("_countryValue").objectReferenceValue =
                ValueRow(content, "Country", 22, Ink);

            // ---- career section ----
            MajorHead(content, "STATS");
            foreach (var line in StatLines)
            {
                if (line.section != null) Head(content, line.section);
                so.FindProperty(line.field).objectReferenceValue = ValueRow(content, line.label, 22, Ink);
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
            brt.sizeDelta = new Vector2(260, 66);
            brt.anchoredPosition = new Vector2(0f, -452f);
            Stretch(Label("Label", back.transform, "BACK", 26, new Color(0.96f, 0.99f, 1f)).rectTransform);

            so.FindProperty("_backButton").objectReferenceValue = back.GetComponent<Button>();
            so.FindProperty("_menuSceneName").stringValue = "MainMenu";
            so.ApplyModifiedPropertiesWithoutUndo();

            // Layout runs on the next frame otherwise, and the scroll would open mid-list.
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScene();
            Debug.Log($"[ProfileBuilder] built {ScenePath}.");
        }

        /// <summary>Viewport + masked, auto-sizing content column. Returns the content transform.</summary>
        private static Transform BuildScroll(Transform window, out ScrollRect scroll)
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(window, false);
            var srt = (RectTransform)scrollGo.transform;
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            // Clear of the title the skinner drops in at the top, and off the panel edges.
            srt.offsetMin = new Vector2(18f, 22f);
            srt.offsetMax = new Vector2(-18f, -86f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            Stretch((RectTransform)viewport.transform);
            var vpImg = viewport.GetComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.01f);   // Mask needs a graphic; this one is invisible
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(0f, 0f);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 2f;
            vlg.padding = new RectOffset(10, 10, 4, 12);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // The fitter is what makes the column as tall as its rows, which is what gives
            // the ScrollRect something to scroll. Without it the content stays zero-height
            // and the list simply does not move.
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = crt;
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            return content.transform;
        }

        private static void BuildNameRow(Transform content, SerializedObject so)
        {
            var row = Row(content, "NicknameRow", RowHeight + 8f);

            var label = Label("Label", row, "Nickname", 22, InkDim);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(0f, 1f); lrt.pivot = new Vector2(0f, 0.5f);
            lrt.sizeDelta = new Vector2(200f, 0f); lrt.anchoredPosition = new Vector2(14f, 0f);
            label.alignment = TextAnchor.MiddleLeft;

            // ---- input field ----
            var fieldGo = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            fieldGo.transform.SetParent(row, false);
            var fieldImg = fieldGo.GetComponent<Image>();
            fieldImg.sprite = Panel;
            fieldImg.type = Image.Type.Sliced;
            fieldImg.color = new Color(0.10f, 0.15f, 0.22f, 0.95f);
            var frt = (RectTransform)fieldGo.transform;
            frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(1f, 1f); frt.pivot = new Vector2(0.5f, 0.5f);
            frt.offsetMin = new Vector2(214f, 6f); frt.offsetMax = new Vector2(-176f, -6f);

            var fieldText = Label("Text", fieldGo.transform, "", 22, Ink);
            fieldText.alignment = TextAnchor.MiddleLeft;
            fieldText.supportRichText = false;
            var ftrt = fieldText.rectTransform;
            ftrt.anchorMin = Vector2.zero; ftrt.anchorMax = Vector2.one;
            ftrt.offsetMin = new Vector2(12f, 2f); ftrt.offsetMax = new Vector2(-12f, -2f);

            var placeholder = Label("Placeholder", fieldGo.transform, "pick a name", 22, Faint);
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.fontStyle = FontStyle.Italic;
            var prt = placeholder.rectTransform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(12f, 2f); prt.offsetMax = new Vector2(-12f, -2f);

            var input = fieldGo.GetComponent<InputField>();
            input.textComponent = fieldText;
            input.placeholder = placeholder;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 16;

            // ---- save ----
            var saveGo = new GameObject("SaveButton", typeof(RectTransform), typeof(Image), typeof(Button));
            saveGo.transform.SetParent(row, false);
            var saveImg = saveGo.GetComponent<Image>();
            saveImg.sprite = Panel;
            saveImg.type = Image.Type.Sliced;
            saveImg.color = new Color(0.6f, 0.84f, 1f, 0.92f);
            var srt2 = (RectTransform)saveGo.transform;
            srt2.anchorMin = new Vector2(1f, 0f); srt2.anchorMax = new Vector2(1f, 1f); srt2.pivot = new Vector2(1f, 0.5f);
            srt2.sizeDelta = new Vector2(156f, -10f); srt2.anchoredPosition = new Vector2(-14f, 0f);
            Stretch(Label("Label", saveGo.transform, "SAVE", 20, new Color(0.96f, 0.99f, 1f)).rectTransform);

            // ---- status line under the field ----
            var statusRow = Row(content, "NameStatusRow", 28f);
            var status = Label("NameStatus", statusRow, "", 17, Faint);
            var strt = (RectTransform)status.transform;
            strt.anchorMin = new Vector2(0f, 0f); strt.anchorMax = new Vector2(1f, 1f); strt.pivot = new Vector2(0f, 0.5f);
            strt.offsetMin = new Vector2(216f, 0f); strt.offsetMax = new Vector2(-14f, 0f);
            status.alignment = TextAnchor.MiddleLeft;
            status.fontStyle = FontStyle.Italic;

            so.FindProperty("_nameInput").objectReferenceValue = input;
            so.FindProperty("_saveButton").objectReferenceValue = saveGo.GetComponent<Button>();
            so.FindProperty("_nameStatus").objectReferenceValue = status;
        }

        // ---------------------------------------------------------------- primitives

        /// <summary>A fixed-height row in the layout column. Children position themselves inside it.</summary>
        private static Transform Row(Transform content, string name, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(content, false);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return go.transform;
        }

        /// <summary>
        /// A centred divider for the screen's two halves. Bigger and centred so ACCOUNT and
        /// STATS read as peers, with the small left-aligned <see cref="Head"/> rows sitting
        /// under them as subdivisions rather than competing with them.
        /// </summary>
        private static void MajorHead(Transform content, string text)
        {
            var row = Row(content, text + "Divider", HeadHeight + 22f);
            var t = Label("Label", row, text, 28, Ink);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(14f, 0f); rt.offsetMax = new Vector2(-14f, -8f);
            t.alignment = TextAnchor.LowerCenter;
        }

        private static void Head(Transform content, string text)
        {
            var row = Row(content, text + "Head", HeadHeight);
            var t = Label("Label", row, text, 20, Heading);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(14f, 0f); rt.offsetMax = new Vector2(-14f, -6f);
            t.alignment = TextAnchor.LowerLeft;
        }

        /// <summary>A label on the left and a value on the right. Returns the value label.</summary>
        private static Text ValueRow(Transform content, string label, int valueSize, Color valueColour)
        {
            var row = Row(content, label.Replace(" ", "") + "Row", RowHeight);

            var l = Label("Label", row, label, 22, InkDim);
            var lrt = (RectTransform)l.transform;
            lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(0f, 1f); lrt.pivot = new Vector2(0f, 0.5f);
            lrt.sizeDelta = new Vector2(260f, 0f); lrt.anchoredPosition = new Vector2(14f, 0f);
            l.alignment = TextAnchor.MiddleLeft;

            var v = Label("Value", row, "–", valueSize, valueColour);
            var vrt = (RectTransform)v.transform;
            vrt.anchorMin = new Vector2(0f, 0f); vrt.anchorMax = new Vector2(1f, 1f); vrt.pivot = new Vector2(1f, 0.5f);
            vrt.offsetMin = new Vector2(280f, 0f); vrt.offsetMax = new Vector2(-14f, 0f);
            v.alignment = TextAnchor.MiddleRight;
            return v;
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

        [MenuItem("SpaceSurvivors/Build/Main-Menu Profile button")]
        private static void BuildMenuButton()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

            var all = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var anchor = all.FirstOrDefault(b => b.name == "HangarButton")
                         ?? all.FirstOrDefault(b => b.name == "ShopButton");
            var parent = anchor != null ? anchor.transform.parent
                : Object.FindFirstObjectByType<Canvas>().transform;

            // The screen was called Stats before it grew the account section.
            foreach (var stale in new[] { "ProfileButton", "StatsButton" })
            {
                var existing = parent.Find(stale);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
            }

            var go = new GameObject("ProfileButton",
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

            Stretch(Label("Label", go.transform, "PROFILE", 30, new Color(0.95f, 0.98f, 1f)).rectTransform);

            var lso = new SerializedObject(go.GetComponent<LoadSceneButton>());
            lso.FindProperty("_sceneName").stringValue = "Profile";
            lso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ProfileBuilder] added PROFILE button to MainMenu. Re-run the M20 redesign to style it.");
        }

        /// <summary>Register Profile and drop the Stats entry it replaced.</summary>
        private static void RegisterScene()
        {
            var list = EditorBuildSettings.scenes.ToList();
            list.RemoveAll(s => s.path == OldStatsScene);
            if (list.All(s => s.path != ScenePath))
                list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OldStatsScene) != null)
            {
                AssetDatabase.DeleteAsset(OldStatsScene);
                Debug.Log("[ProfileBuilder] removed the old Stats scene.");
            }
        }
    }
}
