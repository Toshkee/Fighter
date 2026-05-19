using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SamuraiFighter.Match
{
    public class MatchController : MonoBehaviour
    {
        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.rKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                var s = SceneManager.GetActiveScene();
                SceneManager.LoadScene(s.name);
            }
        }
    }
}
