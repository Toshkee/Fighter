using UnityEngine;

namespace SamuraiFighter.Combat
{
    /// <summary>
    /// A living fighting-game camera: follows the midpoint of the two fighters, zooms
    /// in as they close and pulls back as they separate, shakes on impact, punches in
    /// on heavy hits, and snaps to a dramatic close-up on a KO. All smoothing runs on
    /// unscaled time so the camera keeps moving during hitstop / slow-motion.
    /// <see cref="CameraShake"/> forwards its calls here when an instance exists.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BattleCamera : MonoBehaviour
    {
        public static BattleCamera Instance { get; private set; }

        [SerializeField] private Transform _a;
        [SerializeField] private Transform _b;

        [Header("Framing")]
        [SerializeField] private float _minSize = 3.6f;
        [SerializeField] private float _maxSize = 6.0f;
        [SerializeField] private float _margin = 2.6f;
        [SerializeField] private float _baseY = -0.3f;
        [SerializeField] private float _xMin = -8f;
        [SerializeField] private float _xMax = 8f;

        [Header("Feel")]
        [SerializeField] private float _posSmooth = 0.14f;
        [SerializeField] private float _sizeSmooth = 0.18f;

        private Camera _cam;
        private Vector3 _posVel;
        private float _sizeVel;
        private float _curSize;

        // shake
        private float _shakeDur, _shakeMag;
        // punch (transient zoom-in)
        private float _punch;
        // ko focus
        private Transform _koTarget;
        private float _koTimer;
        private float _koSize = 3.0f;

        private void Awake()
        {
            Instance = this;
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            _curSize = _cam.orthographicSize > 0f ? _cam.orthographicSize : _maxSize;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Configure(Transform a, Transform b, float xMin, float xMax)
        {
            _a = a; _b = b; _xMin = xMin; _xMax = xMax;
        }

        public void AddShake(float duration, float magnitude)
        {
            _shakeDur = Mathf.Max(_shakeDur, duration);
            _shakeMag = Mathf.Max(_shakeMag, magnitude);
        }

        /// <summary>Transient zoom-in kick for heavy impacts (world units of size reduction).</summary>
        public void AddPunch(float amount)
        {
            _punch = Mathf.Max(_punch, amount);
        }

        public void FocusKo(Transform loser, float holdSeconds = 1.3f)
        {
            _koTarget = loser;
            _koTimer = holdSeconds;
        }

        private void LateUpdate()
        {
            if (_cam == null) return;
            float dt = Time.unscaledDeltaTime;
            float aspect = _cam.aspect > 0.01f ? _cam.aspect : 1.778f;

            Vector2 pa = _a != null ? (Vector2)_a.position : Vector2.zero;
            Vector2 pb = _b != null ? (Vector2)_b.position : Vector2.zero;

            float targetSize;
            Vector2 targetCenter;

            if (_koTimer > 0f && _koTarget != null)
            {
                _koTimer -= dt;
                targetSize = _koSize;
                targetCenter = (Vector2)_koTarget.position + Vector2.up * 0.4f;
            }
            else
            {
                float dx = Mathf.Abs(pa.x - pb.x);
                float dy = Mathf.Abs(pa.y - pb.y);
                float needed = Mathf.Max(dx * 0.5f / aspect, dy * 0.5f) + _margin;
                targetSize = Mathf.Clamp(needed, _minSize, _maxSize);
                targetCenter = new Vector2((pa.x + pb.x) * 0.5f, _baseY + (pa.y + pb.y) * 0.12f);
            }

            // Decay punch and apply it as a temporary zoom-in.
            _punch = Mathf.MoveTowards(_punch, 0f, dt * 4f);
            float renderSize = Mathf.Max(1f, targetSize - _punch);

            _curSize = Mathf.SmoothDamp(_curSize, renderSize, ref _sizeVel, _sizeSmooth, Mathf.Infinity, dt);
            _cam.orthographicSize = _curSize;

            // Clamp horizontally so the view stays within stage bounds (when it fits).
            float halfW = _curSize * aspect;
            float cx = targetCenter.x;
            if (_xMax - _xMin > halfW * 2f)
                cx = Mathf.Clamp(cx, _xMin + halfW, _xMax - halfW);
            else
                cx = (_xMin + _xMax) * 0.5f;

            Vector3 desired = new Vector3(cx, targetCenter.y, -10f);
            Vector3 smoothed = Vector3.SmoothDamp(new Vector3(transform.position.x, transform.position.y, -10f),
                                                  desired, ref _posVel, _posSmooth, Mathf.Infinity, dt);

            // Shake on top of the smoothed follow position.
            Vector2 shakeOffset = Vector2.zero;
            if (_shakeDur > 0f)
            {
                shakeOffset = Random.insideUnitCircle * _shakeMag;
                _shakeDur -= dt;
                if (_shakeDur <= 0f) { _shakeDur = 0f; _shakeMag = 0f; }
            }

            transform.position = new Vector3(smoothed.x + shakeOffset.x, smoothed.y + shakeOffset.y, -10f);
        }
    }
}
