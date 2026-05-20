using UnityEngine;
using UnityEngine.UI;
using SamuraiFighter.Combat;

namespace SamuraiFighter.UI
{
    public class SuperMeterBar : MonoBehaviour
    {
        [SerializeField] private SuperMeter _target;
        [SerializeField] private Image _fill;
        [SerializeField] private bool _rightToLeft;
        [SerializeField] private Color _normalColor = new Color(0.85f, 0.6f, 1f);
        [SerializeField] private Color _readyColor = new Color(1f, 0.85f, 0.2f);

        private float _pulse;

        public void Bind(SuperMeter target, Image fill, bool rightToLeft)
        {
            _target = target;
            _fill = fill;
            _rightToLeft = rightToLeft;
        }

        private void Update()
        {
            if (_target == null || _fill == null) return;
            _fill.fillAmount = _target.Fill;
            _fill.fillOrigin = (int)(_rightToLeft ? Image.OriginHorizontal.Right : Image.OriginHorizontal.Left);
            if (_target.IsFull)
            {
                _pulse += Time.deltaTime * 6f;
                float t = (Mathf.Sin(_pulse) + 1f) * 0.5f;
                _fill.color = Color.Lerp(_normalColor, _readyColor, t);
            }
            else
            {
                _fill.color = _normalColor;
            }
        }
    }
}
