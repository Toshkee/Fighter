using UnityEngine;
using UnityEngine.UI;
using SamuraiFighter.Combat;

namespace SamuraiFighter.UI
{
    /// <summary>
    /// Pulsing red screen-edge vignette that fades in as a watched fighter nears
    /// death. Builds its own full-screen overlay Image so the scene builder only
    /// has to add the component and bind a <see cref="Health"/>.
    /// </summary>
    public class LowHealthVignette : MonoBehaviour
    {
        [SerializeField] private Health _watched;
        [SerializeField, Range(0f, 1f)] private float _threshold = 0.3f;
        [SerializeField] private float _maxAlpha = 0.45f;
        [SerializeField] private float _pulseSpeed = 4f;

        private Image _image;

        public void Bind(Health watched) => _watched = watched;

        private void Awake()
        {
            EnsureOverlay();
        }

        private void EnsureOverlay()
        {
            if (_image != null) return;
            var rt = gameObject.GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _image = gameObject.GetComponent<Image>();
            if (_image == null) _image = gameObject.AddComponent<Image>();
            _image.sprite = BuildVignetteSprite();
            _image.color = new Color(0.7f, 0f, 0f, 0f);
            _image.raycastTarget = false;
        }

        private void Update()
        {
            if (_image == null || _watched == null) return;
            float frac = _watched.MaxHP > 0 ? (float)_watched.CurrentHP / _watched.MaxHP : 1f;
            float danger = _watched.IsDead ? 0f : Mathf.Clamp01((_threshold - frac) / _threshold);
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * _pulseSpeed);
            var c = _image.color;
            c.a = danger * _maxAlpha * pulse;
            _image.color = c;
        }

        /// <summary>Radial sprite: transparent centre, opaque red edges.</summary>
        private static Sprite _cached;
        private static Sprite BuildVignetteSprite()
        {
            if (_cached != null) return _cached;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float r = s * 0.5f;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - r) / r, dy = (y - r) / r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((d - 0.45f) / 0.55f);
                    a = a * a;
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            _cached = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _cached;
        }
    }
}
