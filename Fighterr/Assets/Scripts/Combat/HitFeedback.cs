using UnityEngine;

namespace SamuraiFighter.Combat
{
    public static class HitFeedback
    {
        public static void Spawn(Vector2 position)
        {
            var go = new GameObject("HitSpark");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetSprite();
            sr.color = new Color(1f, 0.95f, 0.5f);
            sr.sortingOrder = 100;
            go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            go.AddComponent<HitSparkAnim>();
        }

        private static Sprite _cached;
        private static Sprite GetSprite()
        {
            if (_cached != null) return _cached;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            _cached = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _cached;
        }
    }

    public class HitSparkAnim : MonoBehaviour
    {
        private float _life = 0.12f;
        private float _age;
        private SpriteRenderer _sr;
        private Vector3 _startScale;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _startScale = transform.localScale;
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            float t = _age / _life;
            if (t >= 1f) { Destroy(gameObject); return; }
            transform.localScale = _startScale * (1f + t * 1.5f);
            var c = _sr.color;
            c.a = 1f - t;
            _sr.color = c;
        }
    }
}
