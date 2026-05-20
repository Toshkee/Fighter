using UnityEngine;

namespace SamuraiFighter.Combat
{
    public class CameraShake : MonoBehaviour
    {
        private static CameraShake _instance;

        private Vector3 _basePos;
        private float _duration;
        private float _magnitude;
        private bool _hasBase;

        public static void Shake(float duration, float magnitude)
        {
            EnsureInstance();
            if (_instance == null) return;
            _instance._duration = Mathf.Max(_instance._duration, duration);
            _instance._magnitude = Mathf.Max(_instance._magnitude, magnitude);
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;
            var cam = Camera.main;
            if (cam == null) return;
            _instance = cam.GetComponent<CameraShake>();
            if (_instance == null) _instance = cam.gameObject.AddComponent<CameraShake>();
        }

        private void LateUpdate()
        {
            if (!_hasBase) { _basePos = transform.localPosition; _hasBase = true; }

            if (_duration > 0f)
            {
                float dt = Time.unscaledDeltaTime;
                Vector2 offset = Random.insideUnitCircle * _magnitude;
                transform.localPosition = _basePos + new Vector3(offset.x, offset.y, 0f);
                _duration -= dt;
                if (_duration <= 0f)
                {
                    _duration = 0f;
                    _magnitude = 0f;
                    transform.localPosition = _basePos;
                }
            }
            else
            {
                _basePos = transform.localPosition;
            }
        }
    }
}
