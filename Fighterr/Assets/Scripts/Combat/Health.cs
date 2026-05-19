using System;
using UnityEngine;

namespace SamuraiFighter.Combat
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int _maxHP = 100;
        [SerializeField] private int _currentHP = 100;

        public int MaxHP => _maxHP;
        public int CurrentHP => _currentHP;
        public bool IsDead => _currentHP <= 0;

        public event Action<int> OnDamaged;
        public event Action OnDied;

        private void Awake()
        {
            _currentHP = _maxHP;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            _currentHP = Mathf.Max(0, _currentHP - amount);
            OnDamaged?.Invoke(amount);
            if (_currentHP == 0) OnDied?.Invoke();
        }
    }
}
