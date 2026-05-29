using UnityEngine;

namespace SamuraiFighter.Combat
{
    /// <summary>
    /// World-space floating text (damage numbers, "GUARD", combo flair) rendered with a
    /// <see cref="TextMesh"/> so it needs no canvas. Pops in, rises, and fades on unscaled
    /// time so it reads during hitstop and slow-motion.
    /// </summary>
    public static class FloatingText
    {
        private static Font _font;

        private static Font GetFont()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }

        public static void Damage(Vector2 pos, int amount, bool heavy)
        {
            Color c = heavy ? new Color(1f, 0.55f, 0.2f) : new Color(1f, 0.92f, 0.5f);
            float size = heavy ? 0.16f : 0.12f;
            Spawn(pos + new Vector2(Random.Range(-0.15f, 0.15f), 0.6f), amount.ToString(), c, size, 0.7f);
        }

        public static void Label(Vector2 pos, string text, Color color, float size = 0.12f)
        {
            Spawn(pos + Vector2.up * 0.6f, text, color, size, 0.7f);
        }

        private static void Spawn(Vector2 pos, string text, Color color, float charSize, float life)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = pos;

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = GetFont();
            tm.fontSize = 64;
            tm.fontStyle = FontStyle.Bold;
            tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && tm.font != null)
            {
                mr.sharedMaterial = tm.font.material;
                mr.sortingOrder = 200;
            }

            go.AddComponent<FloatingTextAnim>().Init(life, Random.Range(-0.4f, 0.4f));
        }
    }

    public class FloatingTextAnim : MonoBehaviour
    {
        private float _life;
        private float _age;
        private float _driftX;
        private TextMesh _tm;
        private Color _base;
        private Vector3 _baseScale;

        public void Init(float life, float driftX)
        {
            _life = Mathf.Max(0.1f, life);
            _driftX = driftX;
            _tm = GetComponent<TextMesh>();
            _base = _tm != null ? _tm.color : Color.white;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _age += dt;
            float t = _age / _life;
            if (t >= 1f) { Destroy(gameObject); return; }

            // Rise + drift.
            transform.position += new Vector3(_driftX * dt, (1.8f - t) * dt, 0f);

            // Pop in big then settle.
            float pop = t < 0.18f ? Mathf.Lerp(1.6f, 1f, t / 0.18f) : 1f;
            transform.localScale = _baseScale * pop;

            // Fade out in the back half.
            if (_tm != null)
            {
                var c = _base;
                c.a = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
                _tm.color = c;
            }
        }
    }
}
