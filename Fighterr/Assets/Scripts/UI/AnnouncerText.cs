using UnityEngine;
using UnityEngine.UI;

namespace SamuraiFighter.UI
{
    /// <summary>
    /// Gives the match banner life: whenever its text changes to something non-empty it
    /// punches in (big → settle) with an overshoot and a brief brightness flash. Polls
    /// the Text so callers (MatchController) need no changes. Runs on unscaled time so
    /// the K.O. callout still animates during slow-motion.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class AnnouncerText : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.4f;
        [SerializeField] private float _startScale = 1.8f;

        private Text _text;
        private string _last = "";
        private float _age;
        private bool _animating;
        private Vector3 _baseScale;
        private Color _baseColor;

        private void Awake()
        {
            _text = GetComponent<Text>();
            _baseScale = transform.localScale;
            _baseColor = _text.color;
        }

        private void Update()
        {
            if (_text == null) return;

            string cur = _text.enabled ? _text.text : "";
            if (cur != _last)
            {
                _last = cur;
                if (!string.IsNullOrEmpty(cur)) { _age = 0f; _animating = true; }
            }

            if (!_animating) return;

            _age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_age / _duration);

            // Ease-out-back style overshoot toward the resting scale.
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float overshoot = Mathf.Sin(t * Mathf.PI) * 0.12f;
            float scale = Mathf.Lerp(_startScale, 1f, eased) + overshoot;
            transform.localScale = _baseScale * scale;

            var c = _baseColor;
            c = Color.Lerp(Color.white, _baseColor, eased);
            _text.color = c;

            if (t >= 1f)
            {
                _animating = false;
                transform.localScale = _baseScale;
                _text.color = _baseColor;
            }
        }
    }
}
