using UnityEngine;

namespace SamuraiFighter.Stages
{
    public enum StageKind { MountainShrine, CrimsonDojo }

    /// <summary>
    /// Builds a stage at runtime and picks between two themes per match. The
    /// Mountain Shrine is fully procedural (gradient sky, sun, mountains, torii,
    /// floor, drifting petals). The Crimson Dojo uses the painted background sprite
    /// (assigned by the scene builder) dressed with floating dust + lantern glow.
    /// If no dojo sprite is assigned it always falls back to the procedural shrine,
    /// so it works asset-free.
    /// </summary>
    public class StageRenderer : MonoBehaviour
    {
        [Header("Stage selection")]
        [Tooltip("Random each match unless set to a specific kind.")]
        [SerializeField] private bool _randomEachMatch = true;
        [SerializeField] private StageKind _kind = StageKind.MountainShrine;
        [SerializeField] private Sprite _dojoBackground;

        [Header("Palette — defaults to a sunset shrine")]
        [SerializeField] private Color _skyTop = new Color(0.12f, 0.10f, 0.25f);
        [SerializeField] private Color _skyBottom = new Color(0.92f, 0.42f, 0.26f);
        [SerializeField] private Color _sun = new Color(1f, 0.85f, 0.55f);
        [SerializeField] private Color _farMountain = new Color(0.30f, 0.20f, 0.35f);
        [SerializeField] private Color _nearMountain = new Color(0.18f, 0.12f, 0.22f);
        [SerializeField] private Color _torii = new Color(0.45f, 0.10f, 0.12f);
        [SerializeField] private Color _floor = new Color(0.16f, 0.12f, 0.14f);
        [SerializeField] private Color _petal = new Color(1f, 0.72f, 0.82f);

        [Header("Layout")]
        [SerializeField] private float _viewWidth = 26f;
        [SerializeField] private float _viewHeight = 12f;
        [SerializeField] private float _floorTopY = -2.5f;

        private void Awake()
        {
            StageKind kind = _kind;
            if (_dojoBackground == null) kind = StageKind.MountainShrine; // can't build dojo without art
            else if (_randomEachMatch) kind = Random.value < 0.5f ? StageKind.MountainShrine : StageKind.CrimsonDojo;

            if (kind == StageKind.CrimsonDojo) BuildDojo();
            else BuildShrine();
        }

        private void BuildDojo()
        {
            // Painted interior, scaled to cover the camera view.
            float spriteH = _dojoBackground.bounds.size.y;
            float spriteW = _dojoBackground.bounds.size.x;
            float scale = _viewHeight / spriteH;
            if (_viewWidth / spriteW > scale) scale = _viewWidth / spriteW;
            var bg = new GameObject("DojoBackground");
            bg.transform.SetParent(transform, false);
            var bgsr = bg.AddComponent<SpriteRenderer>();
            bgsr.sprite = _dojoBackground;
            bgsr.sortingOrder = -100;
            bg.transform.localScale = new Vector3(scale, scale, 1f);

            // Warm lantern glows for depth and life.
            Layer("LanternL", RadialGlow(64), 3.2f, 3.2f, new Vector2(-6f, 2.2f), -90, new Color(1f, 0.7f, 0.35f, 0.5f));
            Layer("LanternR", RadialGlow(64), 3.2f, 3.2f, new Vector2(6f, 2.2f), -90, new Color(1f, 0.7f, 0.35f, 0.5f));

            // Slow floating dust motes (warm, subtle).
            var dustGO = new GameObject("Dust");
            dustGO.transform.SetParent(transform, false);
            var dust = dustGO.AddComponent<DriftingPetals>();
            dust.Init(PetalSprite(), new Color(1f, 0.9f, 0.7f, 0.35f), _viewWidth, _viewHeight, -_viewHeight * 0.5f,
                      minScale: 0.05f, maxScale: 0.12f, minFall: 0.2f, maxFall: 0.6f);
        }

        private void BuildShrine()
        {
            // Sky covers everything.
            Layer("Sky", VerticalGradient(8, 128, _skyBottom, _skyTop), _viewWidth, _viewHeight,
                  new Vector2(0f, 0f), -100, Color.white);

            // Sun glow sitting on the horizon.
            Layer("Sun", RadialGlow(64), 9f, 9f, new Vector2(-1.5f, 1.2f), -95, _sun);

            // Two parallax-style silhouette bands.
            Layer("FarMountains", MountainBand(256, 96, 0.55f, 0.18f, 91), _viewWidth, 6.5f,
                  new Vector2(0f, -0.8f), -85, _farMountain);
            Layer("NearMountains", MountainBand(256, 96, 0.42f, 0.26f, 47), _viewWidth, 5.0f,
                  new Vector2(0f, -1.6f), -80, _nearMountain);

            // Torii gate, aspect-correct so it doesn't distort.
            var torii = ToriiSilhouette(160, 200);
            Layer("Torii", torii, 5.0f, 6.25f, new Vector2(4.0f, 0.4f), -70, _torii);

            // Floor slab from the stand-on surface down to the bottom of view.
            float floorH = (_floorTopY) - (-_viewHeight * 0.5f);
            float floorCenterY = _floorTopY - floorH * 0.5f;
            Layer("Floor", SolidTex(), _viewWidth, floorH, new Vector2(0f, floorCenterY), -20, _floor);
            // Bright surface line.
            Layer("FloorEdge", SolidTex(), _viewWidth, 0.08f, new Vector2(0f, _floorTopY), -19,
                  new Color(0.6f, 0.45f, 0.35f));

            // Ambient drifting petals in front of the background, behind the fighters.
            var petalsGO = new GameObject("Petals");
            petalsGO.transform.SetParent(transform, false);
            var petals = petalsGO.AddComponent<DriftingPetals>();
            petals.Init(PetalSprite(), _petal, _viewWidth, _viewHeight, _floorTopY);
        }

        private SpriteRenderer Layer(string name, Sprite sprite, float worldW, float worldH,
                                     Vector2 pos, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            Vector2 size = sprite.bounds.size;
            if (size.x > 0f && size.y > 0f)
                go.transform.localScale = new Vector3(worldW / size.x, worldH / size.y, 1f);
            return sr;
        }

        // ---- texture generators ----

        private static Sprite _solid;
        private static Sprite SolidTex()
        {
            if (_solid != null) return _solid;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var px = new Color32[4];
            for (int i = 0; i < 4; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px); tex.Apply();
            _solid = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            return _solid;
        }

        private static Sprite VerticalGradient(int w, int h, Color bottom, Color top)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            tex.SetPixels(px); tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite RadialGlow(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - r) / r, dy = (y - r) / r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a; // soft falloff
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px); tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MountainBand(int w, int h, float baseFrac, float amp, int seed)
        {
            var rng = new System.Random(seed);
            float phase1 = (float)rng.NextDouble() * 6.28f;
            float phase2 = (float)rng.NextDouble() * 6.28f;
            var px = new Color[w * h];
            for (int x = 0; x < w; x++)
            {
                float fx = (float)x / w;
                float profile = baseFrac
                                + amp * (0.55f * Mathf.Sin(fx * 6.2831f * 2f + phase1)
                                       + 0.30f * Mathf.Sin(fx * 6.2831f * 5f + phase2)
                                       + 0.15f * Mathf.Sin(fx * 6.2831f * 11f));
                int ridge = Mathf.Clamp((int)(profile * h), 0, h - 1);
                for (int y = 0; y < h; y++)
                    px[y * w + x] = y <= ridge ? Color.white : new Color(1, 1, 1, 0);
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(px); tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite ToriiSilhouette(int w, int h)
        {
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(1, 1, 1, 0);

            void Box(float x0, float x1, float y0, float y1)
            {
                int ix0 = Mathf.Clamp((int)(x0 * w), 0, w), ix1 = Mathf.Clamp((int)(x1 * w), 0, w);
                int iy0 = Mathf.Clamp((int)(y0 * h), 0, h), iy1 = Mathf.Clamp((int)(y1 * h), 0, h);
                for (int y = iy0; y < iy1; y++)
                    for (int x = ix0; x < ix1; x++)
                        px[y * w + x] = Color.white;
            }

            Box(0.28f, 0.36f, 0.0f, 0.86f);  // left pillar
            Box(0.64f, 0.72f, 0.0f, 0.86f);  // right pillar
            Box(0.22f, 0.78f, 0.62f, 0.70f); // nuki (lower beam)
            Box(0.14f, 0.86f, 0.86f, 0.95f); // kasagi (top beam)
            Box(0.18f, 0.82f, 0.95f, 1.0f);  // top cap

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(px); tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite _petalSprite;
        private static Sprite PetalSprite()
        {
            if (_petalSprite != null) return _petalSprite;
            const int s = 16;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            float cx = s * 0.5f, cy = s * 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    // squashed ellipse = petal-ish
                    float dx = (x - cx) / (s * 0.5f);
                    float dy = (y - cy) / (s * 0.32f);
                    float d = dx * dx + dy * dy;
                    px[y * s + x] = new Color(1, 1, 1, Mathf.Clamp01(1f - d));
                }
            tex.SetPixels(px); tex.Apply();
            _petalSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 64f);
            return _petalSprite;
        }
    }
}
