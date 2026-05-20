using System.Collections;
using UnityEngine;

namespace SamuraiFighter.Combat
{
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _duration = 0.08f;

        private Color _baseColor;
        private Coroutine _running;
        private bool _captured;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) { _baseColor = _renderer.color; _captured = true; }
        }

        public void Flash()
        {
            if (_renderer == null) return;
            if (!_captured) { _baseColor = _renderer.color; _captured = true; }
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            _renderer.color = _flashColor;
            yield return new WaitForSecondsRealtime(_duration);
            _renderer.color = _baseColor;
            _running = null;
        }
    }
}
