using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using SamuraiFighter.Managers;

namespace SamuraiFighter.UI
{
    /// <summary>Post-match screen: shows the winner and offers rematch / re-select / menu.</summary>
    public class ResultController : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Button _rematchButton;
        [SerializeField] private Button _selectButton;
        [SerializeField] private Button _menuButton;

        private void Awake()
        {
            if (_rematchButton != null) _rematchButton.onClick.AddListener(Rematch);
            if (_selectButton != null) _selectButton.onClick.AddListener(ReSelect);
            if (_menuButton != null) _menuButton.onClick.AddListener(MainMenu);
        }

        private void Start()
        {
            int winner = GameSession.Instance != null ? GameSession.Instance.LastWinner : 0;
            if (_title != null)
                _title.text = winner == 1 ? "PLAYER WINS" : (winner == 2 ? "CPU WINS" : "DRAW");
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Rematch();
            else if (kb.escapeKey.wasPressedThisFrame) MainMenu();
        }

        public void Rematch() => SceneFlow.Load(SceneFlow.Fight);
        public void ReSelect() => SceneFlow.Load(SceneFlow.CharacterSelect);
        public void MainMenu() => SceneFlow.Load(SceneFlow.MainMenu);
    }
}
