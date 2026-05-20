using UnityEngine;
using UnityEngine.UI;
using SamuraiFighter.Combat;

namespace SamuraiFighter.UI
{
    public class ComboCounter : MonoBehaviour
    {
        [SerializeField] private ComboTracker _tracker;
        [SerializeField] private Text _label;
        [SerializeField] private int _minToShow = 2;
        [SerializeField] private float _popDuration = 0.18f;
        [SerializeField] private float _popScale = 1.4f;

        private RectTransform _rt;
        private float _popTimer;
        private int _lastShown;

        public void Bind(ComboTracker tracker, Text label)
        {
            if (_tracker != null) _tracker.OnCountChanged -= HandleCountChanged;
            _tracker = tracker;
            _label = label;
            if (_tracker != null) _tracker.OnCountChanged += HandleCountChanged;
            _rt = _label != null ? _label.rectTransform : null;
            HandleCountChanged(_tracker != null ? _tracker.Count : 0);
        }

        private void OnEnable()
        {
            if (_tracker != null) _tracker.OnCountChanged += HandleCountChanged;
            _rt = _label != null ? _label.rectTransform : null;
            HandleCountChanged(_tracker != null ? _tracker.Count : 0);
        }

        private void OnDisable()
        {
            if (_tracker != null) _tracker.OnCountChanged -= HandleCountChanged;
        }

        private void HandleCountChanged(int count)
        {
            if (_label == null) return;
            if (count < _minToShow)
            {
                _label.enabled = false;
                _lastShown = 0;
                return;
            }
            _label.enabled = true;
            _label.text = count + " HITS";
            if (count > _lastShown) _popTimer = _popDuration;
            _lastShown = count;
        }

        private void Update()
        {
            if (_rt == null) return;
            if (_popTimer > 0f)
            {
                _popTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(_popTimer / _popDuration);
                float s = 1f + (_popScale - 1f) * t;
                _rt.localScale = new Vector3(s, s, 1f);
            }
            else _rt.localScale = Vector3.one;
        }
    }
}
