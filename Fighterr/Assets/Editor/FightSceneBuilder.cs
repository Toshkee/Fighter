using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using SamuraiFighter.Characters;
using SamuraiFighter.Combat;
using SamuraiFighter.Input;

namespace SamuraiFighter.EditorTools
{
    public static class FightSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Fight.unity";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
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

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var whiteSprite = CreateWhiteSprite();

            BuildGround(whiteSprite, groundLayer);
            BuildWalls();
            BuildPlayer(whiteSprite, groundLayer, hurtboxLayer, actions);
            BuildDummy(whiteSprite, hurtboxLayer);
            FrameCamera();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Built Fight scene at {ScenePath}. Controls: A/D move, Space jump, S crouch, Left Mouse / J / gamepad X = light attack.");
        }

        private static void BuildGround(Sprite sprite, int groundLayer)
        {
            var ground = new GameObject("Ground");
            ground.transform.position = new Vector3(0f, -3f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 1f);
            ground.layer = groundLayer;
            var sr = ground.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.3f, 0.3f, 0.35f);
            ground.AddComponent<BoxCollider2D>();
        }

        private static void BuildPlayer(Sprite sprite, int groundLayer, int hurtboxLayer, InputActionAsset actions)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(-2.5f, 0f, 0f);
            player.transform.localScale = new Vector3(1f, 1.5f, 1f);
            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.85f, 0.2f, 0.2f);
            sr.sortingOrder = 1;

            var rb = player.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            player.AddComponent<BoxCollider2D>();

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, -0.55f, 0f);

            var health = player.AddComponent<Health>();
            AddHurtbox(player, hurtboxLayer);

            var hitboxGO = new GameObject("LightAttackHitbox");
            hitboxGO.transform.SetParent(player.transform, false);
            hitboxGO.transform.localPosition = Vector3.zero;
            var hitbox = hitboxGO.AddComponent<Hitbox>();

            var fighter = player.AddComponent<Fighter>();
            var input = player.AddComponent<PlayerInputHandler>();

            var fSO = new SerializedObject(fighter);
            fSO.FindProperty("_groundCheck").objectReferenceValue = groundCheck.transform;
            fSO.FindProperty("_groundLayer").intValue = 1 << groundLayer;
            fSO.FindProperty("_walkSpeed").floatValue = 5f;
            fSO.FindProperty("_jumpForce").floatValue = 12f;
            fSO.FindProperty("_groundCheckRadius").floatValue = 0.15f;
            fSO.FindProperty("_facingRight").boolValue = true;
            fSO.FindProperty("_lightAttackHitbox").objectReferenceValue = hitbox;
            fSO.ApplyModifiedPropertiesWithoutUndo();

            var hSO = new SerializedObject(hitbox);
            hSO.FindProperty("_owner").objectReferenceValue = fighter;
            hSO.FindProperty("_hurtboxLayer").intValue = 1 << hurtboxLayer;
            hSO.FindProperty("_size").vector2Value = new Vector2(1.5f, 1f);
            hSO.ApplyModifiedPropertiesWithoutUndo();

            if (actions != null)
            {
                var iSO = new SerializedObject(input);
                iSO.FindProperty("_actions").objectReferenceValue = actions;
                iSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BuildDummy(Sprite sprite, int hurtboxLayer)
        {
            var dummy = new GameObject("Dummy");
            dummy.transform.position = new Vector3(2.5f, 0f, 0f);
            dummy.transform.localScale = new Vector3(1f, 1.5f, 1f);
            var sr = dummy.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.25f, 0.45f, 0.85f);
            sr.sortingOrder = 1;

            var rb = dummy.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            dummy.AddComponent<BoxCollider2D>();

            dummy.AddComponent<Health>();
            AddHurtbox(dummy, hurtboxLayer);
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
            hb.AddComponent<Hurtbox>();
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
