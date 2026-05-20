using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using SamuraiFighter.Characters;
using SamuraiFighter.Combat;
using Object = UnityEngine.Object;
using SamuraiFighter.Input;
using SamuraiFighter.Match;
using SamuraiFighter.UI;

namespace SamuraiFighter.EditorTools
{
    public static class FightSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Fight.unity";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string FireballPrefabPath = "Assets/Prefabs/Fireball.prefab";
        private const string GroundLayerName = "Ground";
        private const string HurtboxLayerName = "Hurtbox";

        [MenuItem("Fighter/Build Fight Scene")]
        public static void Build()
        {
            EnsureLayer(GroundLayerName);
            EnsureLayer(HurtboxLayerName);
            int groundLayer = LayerMask.NameToLayer(GroundLayerName);
            int hurtboxLayer = LayerMask.NameToLayer(HurtboxLayerName);

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
            {
                Debug.LogError($"FightSceneBuilder: could not find {InputActionsPath}.");
            }

            var fireballPrefab = EnsureFireballPrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var whiteSprite = CreateWhiteSprite();

            BuildBackground();
            BuildGround(whiteSprite, groundLayer);
            BuildWalls();
            var fighters = BuildFighters(whiteSprite, groundLayer, hurtboxLayer, actions, fireballPrefab);
            var hud = BuildHUD(whiteSprite, fighters.playerHealth, fighters.dummyHealth, fighters.playerMeter, fighters.dummyMeter);
            FrameCamera();
            BuildMatchController(fighters, hud);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"Built Fight scene at {ScenePath}. Controls: A/D move, W jump, S crouch, Space light attack, E heavy attack, R restart.");
        }

        private static void BuildGround(Sprite sprite, int groundLayer)
        {
            var ground = new GameObject("Ground");
            ground.transform.position = new Vector3(0f, -3f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 1f);
            ground.layer = groundLayer;
            ground.AddComponent<BoxCollider2D>();
        }

        public struct FightersRefs
        {
            public Health playerHealth;
            public Fighter playerFighter;
            public PlayerInputHandler playerInput;
            public SuperMeter playerMeter;
            public Health dummyHealth;
            public Fighter dummyFighter;
            public DummyAI dummyAI;
            public SuperMeter dummyMeter;
        }

        private static FightersRefs BuildFighters(Sprite sprite, int groundLayer, int hurtboxLayer, InputActionAsset actions, GameObject fireballPrefab)
        {
            var (pHealth, pFighter, pInput, pMeter) = BuildPlayer(sprite, groundLayer, hurtboxLayer, actions, fireballPrefab);
            var (dHealth, dFighter, dAI, dMeter) = BuildDummy(sprite, hurtboxLayer, pFighter, fireballPrefab);
            return new FightersRefs
            {
                playerHealth = pHealth, playerFighter = pFighter, playerInput = pInput, playerMeter = pMeter,
                dummyHealth = dHealth, dummyFighter = dFighter, dummyAI = dAI, dummyMeter = dMeter
            };
        }

        private static (Health health, Fighter fighter, PlayerInputHandler input, SuperMeter meter) BuildPlayer(Sprite sprite, int groundLayer, int hurtboxLayer, InputActionAsset actions, GameObject fireballPrefab)
        {
            var clips = LoadSamuraiClips();
            var player = new GameObject("Player");
            player.transform.position = new Vector3(-2.5f, 0f, 0f);
            player.transform.localScale = Vector3.one;
            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = FirstSprite(clips) ?? sprite;
            sr.color = Color.white;
            sr.sortingOrder = 1;

            var rb = player.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.gravityScale = 3f;
            var bodyCol = player.AddComponent<BoxCollider2D>();
            bodyCol.size = new Vector2(0.6f, 1.5f);
            bodyCol.offset = new Vector2(0f, 0f);

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, -0.75f, 0f);

            var health = player.AddComponent<Health>();
            var meter = player.AddComponent<SuperMeter>();
            AddHurtbox(player, hurtboxLayer);

            var lightGO = new GameObject("LightAttackHitbox");
            lightGO.transform.SetParent(player.transform, false);
            lightGO.transform.localPosition = Vector3.zero;
            var lightHitbox = lightGO.AddComponent<Hitbox>();

            var heavyGO = new GameObject("HeavyAttackHitbox");
            heavyGO.transform.SetParent(player.transform, false);
            heavyGO.transform.localPosition = Vector3.zero;
            var heavyHitbox = heavyGO.AddComponent<Hitbox>();

            var fighter = player.AddComponent<Fighter>();
            var input = player.AddComponent<PlayerInputHandler>();

            var fSO = new SerializedObject(fighter);
            fSO.FindProperty("_groundCheck").objectReferenceValue = groundCheck.transform;
            fSO.FindProperty("_groundLayer").intValue = 1 << groundLayer;
            fSO.FindProperty("_walkSpeed").floatValue = 5f;
            fSO.FindProperty("_jumpForce").floatValue = 8f;
            fSO.FindProperty("_groundCheckRadius").floatValue = 0.15f;
            fSO.FindProperty("_facingRight").boolValue = true;
            fSO.FindProperty("_lightAttackHitbox").objectReferenceValue = lightHitbox;
            fSO.FindProperty("_heavyAttackHitbox").objectReferenceValue = heavyHitbox;
            fSO.FindProperty("_fireballPrefab").objectReferenceValue = fireballPrefab;
            fSO.FindProperty("_health").objectReferenceValue = health;
            fSO.FindProperty("_superMeter").objectReferenceValue = meter;
            var playerHitFlash = player.AddComponent<HitFlash>();
            var playerFlashSO = new SerializedObject(playerHitFlash);
            playerFlashSO.FindProperty("_renderer").objectReferenceValue = sr;
            playerFlashSO.ApplyModifiedPropertiesWithoutUndo();
            fSO.FindProperty("_hitFlash").objectReferenceValue = playerHitFlash;
            fSO.ApplyModifiedPropertiesWithoutUndo();

            ConfigureHitbox(lightHitbox, fighter, hurtboxLayer, new Vector2(1.5f, 1f));
            ConfigureHitbox(heavyHitbox, fighter, hurtboxLayer, new Vector2(1.9f, 1.1f));

            AttachSpriteAnimator(player, sr, fighter, clips);

            if (actions != null)
            {
                var iSO = new SerializedObject(input);
                iSO.FindProperty("_actions").objectReferenceValue = actions;
                iSO.ApplyModifiedPropertiesWithoutUndo();
            }
            return (health, fighter, input, meter);
        }

        private static void ConfigureHitbox(Hitbox hb, Fighter owner, int hurtboxLayer, Vector2 size)
        {
            var so = new SerializedObject(hb);
            so.FindProperty("_owner").objectReferenceValue = owner;
            so.FindProperty("_hurtboxLayer").intValue = 1 << hurtboxLayer;
            so.FindProperty("_size").vector2Value = size;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static (Health health, Fighter fighter, DummyAI ai, SuperMeter meter) BuildDummy(Sprite sprite, int hurtboxLayer, Fighter playerFighter, GameObject fireballPrefab)
        {
            var clips = LoadSamuraiClips();
            var dummy = new GameObject("Dummy");
            dummy.transform.position = new Vector3(2.5f, 0f, 0f);
            dummy.transform.localScale = Vector3.one;
            var sr = dummy.AddComponent<SpriteRenderer>();
            sr.sprite = FirstSprite(clips) ?? sprite;
            sr.color = new Color(0.75f, 0.85f, 1f);
            sr.sortingOrder = 1;

            var rb = dummy.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.gravityScale = 3f;
            var bodyCol = dummy.AddComponent<BoxCollider2D>();
            bodyCol.size = new Vector2(0.6f, 1.5f);
            bodyCol.offset = new Vector2(0f, 0f);

            var health = dummy.AddComponent<Health>();
            var meter = dummy.AddComponent<SuperMeter>();
            AddHurtbox(dummy, hurtboxLayer);

            var lightGO = new GameObject("LightAttackHitbox");
            lightGO.transform.SetParent(dummy.transform, false);
            lightGO.transform.localPosition = Vector3.zero;
            var lightHitbox = lightGO.AddComponent<Hitbox>();

            var heavyGO = new GameObject("HeavyAttackHitbox");
            heavyGO.transform.SetParent(dummy.transform, false);
            heavyGO.transform.localPosition = Vector3.zero;
            var heavyHitbox = heavyGO.AddComponent<Hitbox>();

            var fighter = dummy.AddComponent<Fighter>();
            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(dummy.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, -0.75f, 0f);
            var fSO = new SerializedObject(fighter);
            fSO.FindProperty("_groundCheck").objectReferenceValue = groundCheck.transform;
            fSO.FindProperty("_groundLayer").intValue = 1 << LayerMask.NameToLayer(GroundLayerName);
            fSO.FindProperty("_walkSpeed").floatValue = 4f;
            fSO.FindProperty("_jumpForce").floatValue = 8f;
            fSO.FindProperty("_groundCheckRadius").floatValue = 0.15f;
            fSO.FindProperty("_facingRight").boolValue = false;
            fSO.FindProperty("_lightAttackHitbox").objectReferenceValue = lightHitbox;
            fSO.FindProperty("_heavyAttackHitbox").objectReferenceValue = heavyHitbox;
            fSO.FindProperty("_fireballPrefab").objectReferenceValue = fireballPrefab;
            fSO.FindProperty("_health").objectReferenceValue = health;
            fSO.FindProperty("_superMeter").objectReferenceValue = meter;
            var dummyHitFlash = dummy.AddComponent<HitFlash>();
            var dummyFlashSO = new SerializedObject(dummyHitFlash);
            dummyFlashSO.FindProperty("_renderer").objectReferenceValue = sr;
            dummyFlashSO.ApplyModifiedPropertiesWithoutUndo();
            fSO.FindProperty("_hitFlash").objectReferenceValue = dummyHitFlash;
            fSO.ApplyModifiedPropertiesWithoutUndo();

            ConfigureHitbox(lightHitbox, fighter, hurtboxLayer, new Vector2(1.5f, 1f));
            ConfigureHitbox(heavyHitbox, fighter, hurtboxLayer, new Vector2(1.9f, 1.1f));

            AttachSpriteAnimator(dummy, sr, fighter, clips);

            var ai = dummy.AddComponent<DummyAI>();
            var aiSO = new SerializedObject(ai);
            aiSO.FindProperty("_self").objectReferenceValue = fighter;
            aiSO.FindProperty("_target").objectReferenceValue = playerFighter;
            aiSO.ApplyModifiedPropertiesWithoutUndo();

            dummy.transform.localScale = new Vector3(-1f, 1f, 1f);
            return (health, fighter, ai, meter);
        }

        private static void AddHurtbox(GameObject owner, int hurtboxLayer)
        {
            var hb = new GameObject("Hurtbox");
            hb.transform.SetParent(owner.transform, false);
            hb.transform.localPosition = Vector3.zero;
            hb.layer = hurtboxLayer;
            var col = hb.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
            col.isTrigger = true;
            var hurtbox = hb.AddComponent<Hurtbox>();
            var so = new SerializedObject(hurtbox);
            so.FindProperty("_owner").objectReferenceValue = owner.GetComponent<Fighter>();
            so.FindProperty("_health").objectReferenceValue = owner.GetComponent<Health>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildWalls()
        {
            CreateWall("WallLeft", new Vector3(-9f, 0f, 0f));
            CreateWall("WallRight", new Vector3(9f, 0f, 0f));
        }

        private static void CreateWall(string name, Vector3 position)
        {
            var wall = new GameObject(name);
            wall.transform.position = position;
            wall.transform.localScale = new Vector3(1f, 10f, 1f);
            var col = wall.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }

        public struct HUDRefs
        {
            public RoundTimer timer;
            public Text banner;
            public Image[] p1Pips;
            public Image[] p2Pips;
        }

        private static HUDRefs BuildHUD(Sprite sprite, Health player, Health dummy, SuperMeter playerMeter, SuperMeter dummyMeter)
        {
            var canvasGO = new GameObject("HUD");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            CreateHealthBar(canvasGO.transform, sprite, "P1Bar", player, new Vector2(40f, -40f), TextAnchor.UpperLeft, false);
            CreateHealthBar(canvasGO.transform, sprite, "P2Bar", dummy, new Vector2(-40f, -40f), TextAnchor.UpperRight, true);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var timer = CreateRoundTimer(canvasGO.transform, font);

            CreateComboCounter(canvasGO.transform, font, "P1Combo", dummy, new Vector2(60f, -100f), TextAnchor.UpperLeft, TextAnchor.UpperLeft);
            CreateComboCounter(canvasGO.transform, font, "P2Combo", player, new Vector2(-60f, -100f), TextAnchor.UpperRight, TextAnchor.UpperRight);

            var p1Pips = CreatePips(canvasGO.transform, sprite, "P1Pips", new Vector2(40f, -90f), TextAnchor.UpperLeft);
            var p2Pips = CreatePips(canvasGO.transform, sprite, "P2Pips", new Vector2(-40f, -90f), TextAnchor.UpperRight);

            CreateSuperBar(canvasGO.transform, sprite, "P1Super", playerMeter, new Vector2(40f, -130f), TextAnchor.UpperLeft, false);
            CreateSuperBar(canvasGO.transform, sprite, "P2Super", dummyMeter, new Vector2(-40f, -130f), TextAnchor.UpperRight, true);

            var banner = CreateBanner(canvasGO.transform, font);

            return new HUDRefs { timer = timer, banner = banner, p1Pips = p1Pips, p2Pips = p2Pips };
        }

        private static void CreateSuperBar(Transform parent, Sprite sprite, string name, SuperMeter target, Vector2 offset, TextAnchor anchor, bool rightToLeft)
        {
            var bg = new GameObject(name);
            bg.transform.SetParent(parent, false);
            var bgRT = bg.AddComponent<RectTransform>();
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = sprite;
            bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

            bgRT.sizeDelta = new Vector2(360f, 18f);
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    bgRT.anchorMin = new Vector2(0f, 1f);
                    bgRT.anchorMax = new Vector2(0f, 1f);
                    bgRT.pivot = new Vector2(0f, 1f);
                    break;
                case TextAnchor.UpperRight:
                    bgRT.anchorMin = new Vector2(1f, 1f);
                    bgRT.anchorMax = new Vector2(1f, 1f);
                    bgRT.pivot = new Vector2(1f, 1f);
                    break;
            }
            bgRT.anchoredPosition = offset;

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(bg.transform, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(2f, 2f);
            fillRT.offsetMax = new Vector2(-2f, -2f);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.sprite = sprite;
            fillImg.color = new Color(0.85f, 0.6f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)(rightToLeft ? Image.OriginHorizontal.Right : Image.OriginHorizontal.Left);
            fillImg.fillAmount = 0f;

            var bar = bg.AddComponent<SuperMeterBar>();
            bar.Bind(target, fillImg, rightToLeft);
        }

        private static Image[] CreatePips(Transform parent, Sprite sprite, string name, Vector2 offset, TextAnchor anchorCorner)
        {
            var container = new GameObject(name);
            container.transform.SetParent(parent, false);
            var crt = container.AddComponent<RectTransform>();
            bool right = anchorCorner == TextAnchor.UpperRight;
            crt.anchorMin = new Vector2(right ? 1f : 0f, 1f);
            crt.anchorMax = new Vector2(right ? 1f : 0f, 1f);
            crt.pivot = new Vector2(right ? 1f : 0f, 1f);
            crt.anchoredPosition = offset;
            crt.sizeDelta = new Vector2(120f, 30f);

            var images = new Image[2];
            float gap = 36f;
            float pipSize = 24f;
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Pip" + i);
                go.transform.SetParent(container.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(right ? 1f : 0f, 0.5f);
                rt.anchorMax = new Vector2(right ? 1f : 0f, 0.5f);
                rt.pivot = new Vector2(right ? 1f : 0f, 0.5f);
                float x = (right ? -1f : 1f) * (i * gap);
                rt.anchoredPosition = new Vector2(x, 0f);
                rt.sizeDelta = new Vector2(pipSize, pipSize);
                var img = go.AddComponent<Image>();
                img.sprite = sprite;
                img.color = new Color(0.25f, 0.25f, 0.3f, 0.8f);
                images[i] = img;
            }
            return images;
        }

        private static Text CreateBanner(Transform parent, Font font)
        {
            var go = new GameObject("Banner");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 60f);
            rt.sizeDelta = new Vector2(1400f, 300f);

            var text = go.AddComponent<Text>();
            text.font = font;
            text.text = "";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.95f, 0.7f);
            text.fontSize = 120;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.enabled = false;
            return text;
        }

        private static RoundTimer CreateRoundTimer(Transform parent, Font font)
        {
            var go = new GameObject("RoundTimer");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -30f);
            rt.sizeDelta = new Vector2(200f, 90f);

            var text = go.AddComponent<Text>();
            text.font = font;
            text.text = "99";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 72;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var timer = go.AddComponent<RoundTimer>();
            timer.Bind(text);
            var tSO = new SerializedObject(timer);
            tSO.FindProperty("_autoStart").boolValue = false;
            tSO.ApplyModifiedPropertiesWithoutUndo();
            return timer;
        }

        private static void CreateComboCounter(Transform parent, Font font, string name, Health watchedHealth, Vector2 offset, TextAnchor anchorCorner, TextAnchor textAlign)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            switch (anchorCorner)
            {
                case TextAnchor.UpperLeft:
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    break;
                case TextAnchor.UpperRight:
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    break;
            }
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(400f, 60f);

            var text = go.AddComponent<Text>();
            text.font = font;
            text.text = "";
            text.alignment = textAlign == TextAnchor.UpperLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            text.color = new Color(1f, 0.85f, 0.25f);
            text.fontSize = 44;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.enabled = false;

            var tracker = go.AddComponent<ComboTracker>();
            var tSO = new SerializedObject(tracker);
            tSO.FindProperty("_target").objectReferenceValue = watchedHealth;
            tSO.ApplyModifiedPropertiesWithoutUndo();

            var counter = go.AddComponent<ComboCounter>();
            counter.Bind(tracker, text);
        }

        private static void CreateHealthBar(Transform parent, Sprite sprite, string name, Health target, Vector2 offset, TextAnchor anchor, bool rightToLeft)
        {
            var bg = new GameObject(name);
            bg.transform.SetParent(parent, false);
            var bgRT = bg.AddComponent<RectTransform>();
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = sprite;
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            bgRT.sizeDelta = new Vector2(600f, 40f);
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    bgRT.anchorMin = new Vector2(0f, 1f);
                    bgRT.anchorMax = new Vector2(0f, 1f);
                    bgRT.pivot = new Vector2(0f, 1f);
                    break;
                case TextAnchor.UpperRight:
                    bgRT.anchorMin = new Vector2(1f, 1f);
                    bgRT.anchorMax = new Vector2(1f, 1f);
                    bgRT.pivot = new Vector2(1f, 1f);
                    break;
            }
            bgRT.anchoredPosition = offset;

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(bg.transform, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(4f, 4f);
            fillRT.offsetMax = new Vector2(-4f, -4f);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.sprite = sprite;
            fillImg.color = rightToLeft ? new Color(0.3f, 0.55f, 1f) : new Color(1f, 0.35f, 0.3f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)(rightToLeft ? Image.OriginHorizontal.Right : Image.OriginHorizontal.Left);
            fillImg.fillAmount = 1f;

            var bar = bg.AddComponent<HealthBar>();
            bar.Bind(target, fillImg, rightToLeft);
        }

        private static void FrameCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
        }

        private static Sprite CreateWhiteSprite()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private static void BuildBackground()
        {
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Stages/Dojo/background.png");
            if (bgSprite == null) return;
            var bg = new GameObject("Background");
            bg.transform.position = new Vector3(0f, 0f, 10f);
            var sr = bg.AddComponent<SpriteRenderer>();
            sr.sprite = bgSprite;
            sr.sortingOrder = -10;
            float cam = 5f;
            float spriteH = bgSprite.bounds.size.y;
            float spriteW = bgSprite.bounds.size.x;
            float scale = (cam * 2f) / spriteH;
            float minScaleW = 18f / spriteW;
            if (minScaleW > scale) scale = minScaleW;
            bg.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static GameObject EnsureFireballPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            var fireballSprite = EnsureFireballSpriteAsset();

            var template = new GameObject("Fireball");
            var sr = template.AddComponent<SpriteRenderer>();
            sr.sprite = fireballSprite;
            sr.color = new Color(1f, 0.55f, 0.1f);
            sr.sortingOrder = 2;
            template.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

            var rb = template.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = template.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;

            template.AddComponent<Projectile>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(template, FireballPrefabPath);
            Object.DestroyImmediate(template);
            return prefab;
        }

        private const string FireballSpritePath = "Assets/Art/Effects/Fireball.png";

        private static Sprite EnsureFireballSpriteAsset()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art/Effects"))
                AssetDatabase.CreateFolder("Assets/Art", "Effects");

            if (!System.IO.File.Exists(FireballSpritePath))
            {
                const int size = 16;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color32[size * size];
                float r = size * 0.5f - 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - r, dy = y - r;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                        byte a = (byte)(d <= 1f ? Mathf.Clamp01(1f - d * d) * 255f : 0);
                        px[y * size + x] = new Color32(255, 255, 255, a);
                    }
                tex.SetPixels32(px);
                tex.Apply();
                System.IO.File.WriteAllBytes(FireballSpritePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(FireballSpritePath, ImportAssetOptions.ForceSynchronousImport);
            }

            var importer = AssetImporter.GetAtPath(FireballSpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(FireballSpritePath);
        }

        private static void BuildMatchController(FightersRefs f, HUDRefs h)
        {
            var go = new GameObject("MatchController");
            var mc = go.AddComponent<MatchController>();
            mc.Configure(f.playerFighter, f.dummyFighter, f.playerHealth, f.dummyHealth,
                         f.playerInput, f.dummyAI, h.timer, h.banner, h.p1Pips, h.p2Pips);
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++) if (scenes[i].path == path) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static List<SpriteAnimator.Clip> LoadSamuraiClips()
        {
            var list = new List<SpriteAnimator.Clip>();
            AddClip(list, "Assets/Art/Characters/Samurai1/Idle", FighterState.Idle, AttackKind.None, 10f, true);
            AddClip(list, "Assets/Art/Characters/Samurai1/Walk", FighterState.Walk, AttackKind.None, 12f, true);
            AddClip(list, "Assets/Art/Characters/Samurai1/Jump", FighterState.Jump, AttackKind.None, 12f, false);
            AddClip(list, "Assets/Art/Characters/Samurai1/LightAttack", FighterState.Attack, AttackKind.Light, 10f, false);
            AddClip(list, "Assets/Art/Characters/Samurai1/HeavyAttack", FighterState.Attack, AttackKind.Heavy, 10.5f, false);
            AddClip(list, "Assets/Art/Characters/Samurai1/Fireball", FighterState.Attack, AttackKind.Fireball, 10f, false);
            AddClip(list, "Assets/Art/Characters/Samurai1/TakingPunch", FighterState.Hit, AttackKind.None, 12f, false);
            AddClip(list, "Assets/Art/Characters/Samurai1/Block", FighterState.Block, AttackKind.None, 18f, false);
            return list;
        }

        private static void AddClip(List<SpriteAnimator.Clip> list, string folder, FighterState state, AttackKind kind, float fps, bool loop)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            var paths = new List<string>();
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.StartsWith(folder + "/")) paths.Add(p);
            }
            paths.Sort(System.StringComparer.Ordinal);
            var frames = new List<Sprite>();
            foreach (var p in paths)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) frames.Add(s);
            }
            if (frames.Count == 0) return;
            list.Add(new SpriteAnimator.Clip { state = state, attackKind = kind, fps = fps, loop = loop, frames = frames.ToArray() });
        }

        private static Sprite FirstSprite(List<SpriteAnimator.Clip> clips)
        {
            foreach (var c in clips)
                if (c.state == FighterState.Idle && c.frames != null && c.frames.Length > 0) return c.frames[0];
            foreach (var c in clips)
                if (c.frames != null && c.frames.Length > 0) return c.frames[0];
            return null;
        }

        private static void AttachSpriteAnimator(GameObject owner, SpriteRenderer sr, Fighter fighter, List<SpriteAnimator.Clip> clips)
        {
            var anim = owner.AddComponent<SpriteAnimator>();
            anim.SetClips(clips);
            var so = new SerializedObject(anim);
            so.FindProperty("_fighter").objectReferenceValue = fighter;
            so.FindProperty("_renderer").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(anim);
        }

        private static void EnsureLayer(string name)
        {
            if (LayerMask.NameToLayer(name) != -1) return;
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = name;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }
            Debug.LogError($"No free layer slot to create {name} layer.");
        }
    }
}
