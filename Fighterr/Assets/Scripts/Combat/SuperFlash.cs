using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SamuraiFighter.Combat
{
    public class SuperFlash : MonoBehaviour
    {
        private static SuperFlash _instance;
        private Image _flashImage;
        private Coroutine _current;

        public static void Trigger(float duration, Action onComplete)
        {
            EnsureInstance();
            if (_instance._current != null) _instance.StopCoroutine(_instance._current);
            _instance._current = _instance.StartCoroutine(_instance.Run(duration, onComplete));
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;
            var go = new GameObject("SuperFlash");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SuperFlash>();

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var imgGO = new GameObject("Flash");
            imgGO.transform.SetParent(go.transform, false);
            var rt = imgGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _instance._flashImage = imgGO.AddComponent<Image>();
            _instance._flashImage.color = new Color(1f, 1f, 1f, 0f);
            _instance._flashImage.raycastTarget = false;
        }

        private IEnumerator Run(float duration, Action onComplete)
        {
            float prevScale = Time.timeScale;
            Time.timeScale = 0f;
            float t = 0f;
            float half = duration * 0.5f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = t < half ? Mathf.Lerp(0f, 0.9f, t / half) : Mathf.Lerp(0.9f, 0f, (t - half) / half);
                _flashImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                yield return null;
            }
            _flashImage.color = new Color(1f, 1f, 1f, 0f);
            Time.timeScale = prevScale <= 0f ? 1f : prevScale;
            _current = null;
            onComplete?.Invoke();
        }
    }
}
