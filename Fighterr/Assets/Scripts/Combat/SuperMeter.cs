using System;
using UnityEngine;

namespace SamuraiFighter.Combat
{
    public class SuperMeter : MonoBehaviour
    {
        [SerializeField] private int _max = 100;
        [SerializeField] private int _current = 0;

        public int Max => _max;
        public int Current => _current;
        public float Fill => _max > 0 ? (float)_current / _max : 0f;
        public bool IsFull => _current >= _max;

        public event Action<int> OnChanged;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            int prev = _current;
            _current = Mathf.Clamp(_current + amount, 0, _max);
            if (_current != prev) OnChanged?.Invoke(_current);
        }

        public void Drain(int amount)
        {
            if (amount <= 0) return;
            int prev = _current;
            _current = Mathf.Clamp(_current - amount, 0, _max);
            if (_current != prev) OnChanged?.Invoke(_current);
        }

        public void Reset()
        {
            if (_current == 0) return;
            _current = 0;
            OnChanged?.Invoke(0);
        }
    }
}
