using UnityEngine;

namespace SamuraiFighter.Stages
{
    /// <summary>Ambient cherry-blossom petals that drift down and sway, recycling at the floor.</summary>
    public class DriftingPetals : MonoBehaviour
    {
        [SerializeField] private int _count = 34;

        private float _width;
        private float _height;
        private float _floorY;

        private Transform[] _petals;
        private float[] _fallSpeed;
        private float[] _swayAmp;
        private float[] _swayFreq;
        private float[] _phase;
        private float[] _spin;

        public void Init(Sprite sprite, Color color, float viewWidth, float viewHeight, float floorTopY,
                         float minScale = 0.12f, float maxScale = 0.26f, float minFall = 0.7f, float maxFall = 1.8f)
        {
            _width = viewWidth;
            _height = viewHeight;
            _floorY = floorTopY;

            _petals = new Transform[_count];
            _fallSpeed = new float[_count];
            _swayAmp = new float[_count];
            _swayFreq = new float[_count];
            _phase = new float[_count];
            _spin = new float[_count];

            for (int i = 0; i < _count; i++)
            {
                var go = new GameObject("Petal");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                var c = color;
                c.a = Random.Range(0.55f, 0.95f);
                sr.color = c;
                sr.sortingOrder = -5; // behind fighters, in front of background
                float scale = Random.Range(minScale, maxScale);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                go.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

                _petals[i] = go.transform;
                _fallSpeed[i] = Random.Range(minFall, maxFall);
                _swayAmp[i] = Random.Range(0.3f, 1.0f);
                _swayFreq[i] = Random.Range(0.5f, 1.6f);
                _phase[i] = Random.Range(0f, 6.28f);
                _spin[i] = Random.Range(-90f, 90f);

                go.transform.localPosition = RandomStart(true);
            }
        }

        private Vector3 RandomStart(bool anywhere)
        {
            float x = Random.Range(-_width * 0.5f, _width * 0.5f);
            float y = anywhere ? Random.Range(_floorY, _height * 0.5f) : _height * 0.5f + Random.Range(0f, 2f);
            return new Vector3(x, y, 0f);
        }

        private void Update()
        {
            if (_petals == null) return;
            float dt = Time.deltaTime;
            float time = Time.time;
            for (int i = 0; i < _petals.Length; i++)
            {
                var p = _petals[i];
                Vector3 pos = p.localPosition;
                pos.y -= _fallSpeed[i] * dt;
                pos.x += Mathf.Sin(time * _swayFreq[i] + _phase[i]) * _swayAmp[i] * dt;
                p.localPosition = pos;
                p.localRotation *= Quaternion.Euler(0, 0, _spin[i] * dt);

                if (pos.y < _floorY || pos.x < -_width * 0.6f || pos.x > _width * 0.6f)
                    p.localPosition = RandomStart(false);
            }
        }
    }
}
