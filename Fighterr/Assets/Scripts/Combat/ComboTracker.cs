using System;
using UnityEngine;

namespace SamuraiFighter.Combat
{
    public class ComboTracker : MonoBehaviour
    {
        [SerializeField] private Health _target;
        [SerializeField] private float _resetSeconds = 1.25f;

        private int _count;
        private float _timer;

        public int Count => _count;
        public event Action<int> OnCountChanged;

        private void OnEnable()
        {
            if (_target != null) _target.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (_target != null) _target.OnDamaged -= HandleDamaged;
        }

        public void Bind(Health target)
        {
            if (_target != null) _target.OnDamaged -= HandleDamaged;
            _target = target;
            if (isActiveAndEnabled && _target != null) _target.OnDamaged += HandleDamaged;
        }

        private void HandleDamaged(int amount)
        {
            _count++;
            _timer = _resetSeconds;
            OnCountChanged?.Invoke(_count);
        }

        private void Update()
        {
            if (_count == 0) return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _count = 0;
                OnCountChanged?.Invoke(0);
            }
        }
    }
}
