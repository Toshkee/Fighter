using System.Collections;
using UnityEngine;

namespace SamuraiFighter.Combat
{
    /// <summary>
    /// The KO drama: a hard freeze-frame, then a stretch of slow-motion, then a smooth
    /// ramp back to full speed. Self-bootstraps like <see cref="HitstopController"/> and
    /// takes sole ownership of <c>Time.timeScale</c> for the duration (it cancels any
    /// active hitstop first), so the two never fight over the clock.
    /// </summary>
    public class KoSequence : MonoBehaviour
    {
        private static KoSequence _instance;
        private Coroutine _current;

        public static void Play()
        {
            EnsureInstance();
            if (_instance._current != null) _instance.StopCoroutine(_instance._current);
            _instance._current = _instance.StartCoroutine(_instance.Run());
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;
            var go = new GameObject("KoSequence");
            _instance = go.AddComponent<KoSequence>();
            DontDestroyOnLoad(go);
        }

        private IEnumerator Run()
        {
            HitstopController.Cancel();

            // 1. Freeze frame.
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.14f);

            // 2. Slow motion.
            Time.timeScale = 0.28f;
            yield return new WaitForSecondsRealtime(0.65f);

            // 3. Ramp back to full speed.
            float t = 0f;
            const float ramp = 0.35f;
            while (t < ramp)
            {
                t += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(0.28f, 1f, t / ramp);
                yield return null;
            }
            Time.timeScale = 1f;
            _current = null;
        }
    }
}
