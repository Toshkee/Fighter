using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using SamuraiFighter.Managers;

namespace SamuraiFighter.UI
{
    /// <summary>Title screen. Play → character select. Mouse buttons + keyboard (Enter/Esc).</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _quitButton;

        private void Awake()
        {
            if (_playButton != null) _playButton.onClick.AddListener(Play);
            if (_quitButton != null) _quitButton.onClick.AddListener(Quit);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Play();
            else if (kb.escapeKey.wasPressedThisFrame) Quit();
        }

        public void Play()
        {
            GameSession.GetOrCreate();
            SceneFlow.Load(SceneFlow.CharacterSelect);
        }

        public void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
