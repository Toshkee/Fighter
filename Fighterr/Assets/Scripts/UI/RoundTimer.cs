using System;
using UnityEngine;
using UnityEngine.UI;

namespace SamuraiFighter.UI
{
    public class RoundTimer : MonoBehaviour
    {
        [SerializeField] private float _roundSeconds = 99f;
        [SerializeField] private Text _label;
        [SerializeField] private bool _autoStart = true;

        private float _remaining;
        private bool _running;

        public float Remaining => _remaining;
        public bool IsRunning => _running;
        public event Action OnTimeout;

        private void Start()
        {
            _remaining = _roundSeconds;
            if (_autoStart) _running = true;
            UpdateLabel();
        }

        public void StartTimer(float seconds)
        {
            _remaining = seconds;
            _running = true;
            UpdateLabel();
        }

        public void Stop() { _running = false; }

        private void Update()
        {
            if (!_running) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                _running = false;
                UpdateLabel();
                OnTimeout?.Invoke();
                return;
            }
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (_label == null) return;
            _label.text = Mathf.CeilToInt(_remaining).ToString();
        }

        public void Bind(Text label) { _label = label; }
    }
}
