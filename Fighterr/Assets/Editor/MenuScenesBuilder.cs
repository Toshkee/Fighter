using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SamuraiFighter.Characters;
using SamuraiFighter.UI;
using Object = UnityEngine.Object;

namespace SamuraiFighter.EditorTools
{
    /// <summary>
    /// Builds the front-end scenes (Main Menu, Character Select, Result) entirely in
    /// code, mirroring <see cref="FightSceneBuilder"/>. Run "Fighter ▶ Build Everything"
    /// to (re)build the whole game and set the correct scene order.
    /// </summary>
    public static class MenuScenesBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        private const string CharacterSelectPath = "Assets/Scenes/CharacterSelect.unity";
        private const string ResultPath = "Assets/Scenes/Result.unity";
        private const string FightPath = "Assets/Scenes/Fight.unity";
        private const string CharactersFolder = "Assets/ScriptableObjects/Characters";

        [MenuItem("Fighter/Build Everything", priority = 0)]
        public static void BuildEverything()
        {
            FightSceneBuilder.Build();   // (re)builds the Fight scene and its characters
            BuildMainMenu();
            BuildCharacterSelect();
            BuildResult();
            SetBuildOrder(MainMenuPath, CharacterSelectPath, FightPath, ResultPath);
            EditorSceneManager.OpenScene(MainMenuPath);
            Debug.Log("Built full game: MainMenu → CharacterSelect → Fight → Result. " +
                      "Open MainMenu.unity and press Play. Menus: Enter/Space confirm, Esc back, mouse clicks work too.");
        }

        [MenuItem("Fighter/Build Menu Scenes", priority = 20)]
        public static void BuildMenusOnly()
        {
            BuildMainMenu();
            BuildCharacterSelect();
            BuildResult();
            SetBuildOrder(MainMenuPath, CharacterSelectPath, FightPath, ResultPath);
            EditorSceneManager.OpenScene(MainMenuPath);
        }

        // ---- Main Menu ----

        private static void BuildMainMenu()
        {
            var scene = NewScene();
            StyleCamera(new Color(0.06f, 0.06f, 0.09f));
            var canvas = CreateCanvas("MainMenuCanvas");
            CreateEventSystem();
            var font = GetFont();

            CreateTitle(canvas.transform, font, "SAMURAI FIGHTER", new Vector2(0f, -180f), 110);
            CreateLabel(canvas.transform, font, "Enter / Space — Play     Esc — Quit",
                        new Vector2(0f, -300f), 30, new Color(0.8f, 0.8f, 0.85f), TextAnchor.MiddleCenter, new Vector2(900f, 50f));

            var play = CreateButton(canvas.transform, font, "PLAY", new Vector2(0f, 10f), new Vector2(360f, 90f));
            var quit = CreateButton(canvas.transform, font, "QUIT", new Vector2(0f, -100f), new Vector2(360f, 90f));

            var ctrl = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("_playButton").objectReferenceValue = play;
            so.FindProperty("_quitButton").objectReferenceValue = quit;
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveScene(scene, MainMenuPath);
        }

        // ---- Character Select ----

        private static void BuildCharacterSelect()
        {
            var scene = NewScene();
            StyleCamera(new Color(0.08f, 0.07f, 0.1f));
            var canvas = CreateCanvas("SelectCanvas");
            CreateEventSystem();
            var font = GetFont();

            CreateTitle(canvas.transform, font, "SELECT YOUR FIGHTER", new Vector2(0f, -90f), 70);

            var roster = LoadRoster();
            int n = Mathf.Max(1, roster.Length);
            var buttons = new Button[roster.Length];
            var frames = new Image[roster.Length];

            const float w = 220f, h = 300f, gap = 50f;
            float total = n * w + (n - 1) * gap;
            float startX = -total / 2f + w / 2f;
            for (int i = 0; i < roster.Length; i++)
            {
                var (btn, frame) = CreatePortrait(canvas.transform, font, roster[i],
                                                  new Vector2(startX + i * (w + gap), 30f), new Vector2(w, h));
                buttons[i] = btn;
                frames[i] = frame;
            }

            var nameLabel = CreateLabel(canvas.transform, font, "", new Vector2(0f, -230f), 56,
                                        new Color(1f, 0.85f, 0.25f), TextAnchor.MiddleCenter, new Vector2(900f, 80f));
            CreateLabel(canvas.transform, font, "← / → choose     Enter confirm     Esc back",
                        new Vector2(0f, -320f), 30, new Color(0.8f, 0.8f, 0.85f), TextAnchor.MiddleCenter, new Vector2(1000f, 50f));

            var ctrl = new GameObject("CharacterSelectController").AddComponent<CharacterSelectController>();
            var so = new SerializedObject(ctrl);
            SetObjectArray(so, "_roster", roster);
            SetObjectArray(so, "_portraitButtons", buttons);
            SetObjectArray(so, "_portraitFrames", frames);
            so.FindProperty("_nameLabel").objectReferenceValue = nameLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveScene(scene, CharacterSelectPath);
        }

        private static (Button, Image) CreatePortrait(Transform parent, Font font, CharacterData c, Vector2 pos, Vector2 size)
        {
            var container = new GameObject("Portrait_" + (c != null ? c.displayName : "?"));
            container.transform.SetParent(parent, false);
            var rt = container.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var frame = container.AddComponent<Image>();
            frame.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

            // Inner tint block standing in for character art.
            var tintGO = new GameObject("Tint");
            tintGO.transform.SetParent(container.transform, false);
            var trt = tintGO.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(12f, 60f);
            trt.offsetMax = new Vector2(-12f, -12f);
            var tintImg = tintGO.AddComponent<Image>();
            tintImg.color = c != null ? c.tint : Color.gray;

            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(container.transform, false);
            var nrt = nameGO.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0f);
            nrt.anchorMax = new Vector2(1f, 0f);
            nrt.pivot = new Vector2(0.5f, 0f);
            nrt.anchoredPosition = new Vector2(0f, 12f);
            nrt.sizeDelta = new Vector2(0f, 44f);
            var nameTxt = nameGO.AddComponent<Text>();
            nameTxt.font = font;
            nameTxt.text = c != null ? c.displayName : "?";
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.color = Color.white;
            nameTxt.fontSize = 30;
            nameTxt.fontStyle = FontStyle.Bold;

            var btn = container.AddComponent<Button>();
            btn.targetGraphic = frame;

            return (btn, frame);
        }

        // ---- Result ----

        private static void BuildResult()
        {
            var scene = NewScene();
            StyleCamera(new Color(0.05f, 0.05f, 0.08f));
            var canvas = CreateCanvas("ResultCanvas");
            CreateEventSystem();
            var font = GetFont();

            var title = CreateLabel(canvas.transform, font, "PLAYER WINS", new Vector2(0f, -160f), 100,
                                    new Color(1f, 0.95f, 0.7f), TextAnchor.MiddleCenter, new Vector2(1400f, 200f));

            var rematch = CreateButton(canvas.transform, font, "REMATCH", new Vector2(0f, 30f), new Vector2(420f, 80f));
            var select = CreateButton(canvas.transform, font, "CHARACTER SELECT", new Vector2(0f, -70f), new Vector2(420f, 80f));
            var menu = CreateButton(canvas.transform, font, "MAIN MENU", new Vector2(0f, -170f), new Vector2(420f, 80f));

            var ctrl = new GameObject("ResultController").AddComponent<ResultController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_rematchButton").objectReferenceValue = rematch;
            so.FindProperty("_selectButton").objectReferenceValue = select;
            so.FindProperty("_menuButton").objectReferenceValue = menu;
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveScene(scene, ResultPath);
        }

        // ---- shared UI helpers ----

        private static Scene NewScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        private static void StyleCamera(Color bg)
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
        }

        private static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static Text CreateTitle(Transform parent, Font font, string text, Vector2 anchoredPos, int fontSize)
        {
            return CreateLabel(parent, font, text, anchoredPos, fontSize, new Color(1f, 0.95f, 0.7f),
                               TextAnchor.MiddleCenter, new Vector2(1600f, 220f), topAnchored: true);
        }

        private static Text CreateLabel(Transform parent, Font font, string text, Vector2 anchoredPos, int fontSize,
                                        Color color, TextAnchor align, Vector2 size, bool topAnchored = false)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            float anchorY = topAnchored ? 1f : 0.5f;
            rt.anchorMin = new Vector2(0.5f, anchorY);
            rt.anchorMax = new Vector2(0.5f, anchorY);
            rt.pivot = new Vector2(0.5f, anchorY);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.alignment = align;
            t.color = color;
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Button CreateButton(Transform parent, Font font, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.75f, 0.75f, 0.9f);
            colors.pressedColor = new Color(1f, 0.85f, 0.4f);
            colors.selectedColor = new Color(0.85f, 0.85f, 1f);
            btn.colors = colors;

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var trt = txtGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var t = txtGO.AddComponent<Text>();
            t.font = font;
            t.text = label;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.fontSize = 40;
            t.fontStyle = FontStyle.Bold;

            return btn;
        }

        private static Font GetFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        private static CharacterData[] LoadRoster()
        {
            var guids = AssetDatabase.FindAssets("t:CharacterData", new[] { CharactersFolder });
            var list = new List<CharacterData>();
            var paths = new List<string>();
            foreach (var g in guids) paths.Add(AssetDatabase.GUIDToAssetPath(g));
            paths.Sort(System.StringComparer.Ordinal);
            foreach (var p in paths)
            {
                var cd = AssetDatabase.LoadAssetAtPath<CharacterData>(p);
                if (cd != null) list.Add(cd);
            }
            return list.ToArray();
        }

        private static void SetObjectArray(SerializedObject so, string propertyName, Object[] values)
        {
            var prop = so.FindProperty(propertyName);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void SaveScene(Scene scene, string path)
        {
            if (!AssetDatabase.IsValidFolder(ScenesFolder))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void SetBuildOrder(params string[] paths)
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in paths)
                list.Add(new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
