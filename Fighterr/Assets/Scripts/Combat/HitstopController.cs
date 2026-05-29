using System.Collections;
using UnityEngine;

namespace SamuraiFighter.Combat
{
    public class HitstopController : MonoBehaviour
    {
        private static HitstopController _instance;
        private Coroutine _current;

        public static void Apply(int frames)
        {
            if (frames <= 0) return;
            EnsureInstance();
            if (_instance._current != null) _instance.StopCoroutine(_instance._current);
            _instance._current = _instance.StartCoroutine(_instance.Run(frames));
        }

        /// <summary>Stop any in-progress hitstop without restoring timescale (the caller takes over).</summary>
        public static void Cancel()
        {
            if (_instance != null && _instance._current != null)
            {
                _instance.StopCoroutine(_instance._current);
                _instance._current = null;
            }
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;
            var go = new GameObject("HitstopController");
            _instance = go.AddComponent<HitstopController>();
            DontDestroyOnLoad(go);
        }

        private IEnumerator Run(int frames)
        {
            float duration = frames / 60f;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            _current = null;
        }
    }
}
