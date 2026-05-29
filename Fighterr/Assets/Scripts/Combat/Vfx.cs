using UnityEngine;

namespace SamuraiFighter.Combat
{
    /// <summary>
    /// Lightweight, asset-free particle bursts built from a single generated
    /// soft-dot sprite. Each call spawns a handful of shards that fly outward,
    /// fade, and self-destruct. Runs on unscaled time so effects keep animating
    /// during hitstop. Replaces the old single-sprite <see cref="HitFeedback"/>.
    /// </summary>
    public static class Vfx
    {
        private static Sprite _dot;

        /// <summary>Bright yellow-white impact burst, biased in the direction of the hit.</summary>
        public static void Hit(Vector2 pos, int facing, float intensity = 1f)
        {
            int count = Mathf.RoundToInt(Mathf.Lerp(6f, 14f, Mathf.Clamp01((intensity - 0.7f) / 0.7f)));
            SpawnBurst(pos, count, new Color(1f, 0.95f, 0.55f), facing, 7f * intensity, 0.55f * intensity, 0.22f);
            SpawnBurst(pos, count / 2, Color.white, facing, 9f * intensity, 0.35f * intensity, 0.14f);
            CoreFlash(pos, intensity);
            if (intensity >= 1.1f) ImpactRing(pos, new Color(1f, 0.85f, 0.4f), 2.4f); // heavy/super
        }

        /// <summary>Expanding ring of light — big-impact flourish.</summary>
        public static void ImpactRing(Vector2 pos, Color color, float maxSize)
        {
            var go = new GameObject("vfxRing");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetRing();
            sr.color = color;
            sr.sortingOrder = 119;
            go.AddComponent<ScaleFadeFx>().Init(0.4f, maxSize, 0.28f);
        }

        /// <summary>Brief bright core at the point of contact.</summary>
        public static void CoreFlash(Vector2 pos, float intensity)
        {
            var go = new GameObject("vfxFlash");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDot();
            sr.color = new Color(1f, 1f, 1f, 0.9f);
            sr.sortingOrder = 121;
            go.AddComponent<ScaleFadeFx>().Init(0.3f * intensity, 1.2f * intensity, 0.12f);
        }

        /// <summary>Gold upward sparkle shower for round wins.</summary>
        public static void Celebrate(Vector2 pos)
        {
            SpawnBurst(pos, 24, new Color(1f, 0.85f, 0.3f), 0, 6f, 0.5f, 0.8f, upBias: 1.3f, gravity: -3f);
            SpawnBurst(pos, 12, Color.white, 0, 8f, 0.35f, 0.6f, upBias: 1.1f, gravity: -3f);
        }

        /// <summary>Cyan sparks when an attack is blocked.</summary>
        public static void Block(Vector2 pos, int facing)
        {
            SpawnBurst(pos, 7, new Color(0.5f, 0.8f, 1f), facing, 5f, 0.4f, 0.2f);
        }

        /// <summary>Big white-gold flash burst on a successful parry.</summary>
        public static void Parry(Vector2 pos)
        {
            SpawnBurst(pos, 16, new Color(1f, 0.97f, 0.7f), 0, 8f, 0.6f, 0.35f);
            SpawnBurst(pos, 10, Color.white, 0, 11f, 0.4f, 0.22f);
        }

        /// <summary>Grey ground dust (landings, dashes).</summary>
        public static void Dust(Vector2 pos, int facing)
        {
            SpawnBurst(pos, 6, new Color(0.75f, 0.72f, 0.66f, 0.9f), -facing, 3.5f, 0.45f, 0.3f, upBias: 0.4f, gravity: -2f);
        }

        private static void SpawnBurst(Vector2 pos, int count, Color color, int facing, float speed,
                                       float size, float life, float upBias = 0f, float gravity = -9f)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("vfx");
                go.transform.position = pos;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetDot();
                sr.color = color;
                sr.sortingOrder = 120;

                Vector2 dir = Random.insideUnitCircle.normalized;
                if (facing != 0) dir.x = Mathf.Abs(dir.x) * Mathf.Sign(facing); // bias along hit direction
                dir.y += upBias;
                Vector2 vel = dir.normalized * speed * Random.Range(0.5f, 1.2f);

                var p = go.AddComponent<VfxParticle>();
                p.Init(vel, size * Random.Range(0.6f, 1.1f), life * Random.Range(0.7f, 1.1f), gravity);
            }
        }

        private static Sprite GetDot()
        {
            if (_dot != null) return _dot;
            const int s = 8;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float r = s * 0.5f - 0.5f;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - r, dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    byte a = (byte)(Mathf.Clamp01(1f - d * d) * 255f);
                    px[y * s + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            _dot = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _dot;
        }

        private static Sprite _ring;
        private static Sprite GetRing()
        {
            if (_ring != null) return _ring;
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float r = s * 0.5f - 0.5f;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - r, dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / r; // 0 center .. 1 edge
                    // bright annulus peaking near the rim
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) / 0.2f);
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            _ring = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _ring;
        }
    }

    /// <summary>Grows from a start size to an end size while fading out. For rings/flashes.</summary>
    public class ScaleFadeFx : MonoBehaviour
    {
        private float _startSize, _endSize, _life, _age;
        private SpriteRenderer _sr;
        private float _baseAlpha;

        public void Init(float startSize, float endSize, float life)
        {
            _startSize = Mathf.Max(0.01f, startSize);
            _endSize = endSize;
            _life = Mathf.Max(0.05f, life);
            _sr = GetComponent<SpriteRenderer>();
            _baseAlpha = _sr != null ? _sr.color.a : 1f;
            transform.localScale = Vector3.one * _startSize;
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            float t = _age / _life;
            if (t >= 1f) { Destroy(gameObject); return; }
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            transform.localScale = Vector3.one * Mathf.Lerp(_startSize, _endSize, eased);
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = _baseAlpha * (1f - t);
                _sr.color = c;
            }
        }
    }

    public class VfxParticle : MonoBehaviour
    {
        private Vector2 _vel;
        private float _life;
        private float _age;
        private float _gravity;
        private SpriteRenderer _sr;
        private Vector3 _startScale;

        public void Init(Vector2 vel, float size, float life, float gravity)
        {
            _vel = vel;
            _life = Mathf.Max(0.05f, life);
            _gravity = gravity;
            transform.localScale = Vector3.one * size;
            _startScale = transform.localScale;
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _age += dt;
            float t = _age / _life;
            if (t >= 1f) { Destroy(gameObject); return; }

            _vel.y += _gravity * dt;
            transform.position += (Vector3)(_vel * dt);
            transform.localScale = _startScale * (1f - 0.6f * t);
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = 1f - t;
                _sr.color = c;
            }
        }
    }
}
