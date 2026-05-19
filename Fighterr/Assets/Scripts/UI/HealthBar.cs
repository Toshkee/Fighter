using UnityEngine;
using UnityEngine.UI;
using SamuraiFighter.Combat;

namespace SamuraiFighter.UI
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Health _target;
        [SerializeField] private Image _fill;
        [SerializeField] private bool _rightToLeft;

        public void Bind(Health target, Image fill, bool rightToLeft)
        {
            _target = target;
            _fill = fill;
            _rightToLeft = rightToLeft;
        }

        private void Update()
        {
            if (_target == null || _fill == null) return;
            float t = _target.MaxHP > 0 ? (float)_target.CurrentHP / _target.MaxHP : 0f;
            _fill.fillAmount = t;
            if (_rightToLeft) _fill.fillOrigin = (int)Image.OriginHorizontal.Right;
            else _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }
}
