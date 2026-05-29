using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using SamuraiFighter.Characters;
using SamuraiFighter.Managers;
using SamuraiFighter.Utils;

namespace SamuraiFighter.UI
{
    /// <summary>
    /// Pick your fighter. Left/Right (or A/D) moves the cursor, Enter/Space or a mouse
    /// click confirms. The CPU opponent is chosen at random. Roster + UI references are
    /// assigned by the scene builder.
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        [SerializeField] private CharacterData[] _roster;
        [SerializeField] private Button[] _portraitButtons;
        [SerializeField] private Image[] _portraitFrames;
        [SerializeField] private Text _nameLabel;

        [SerializeField] private Color _frameIdle = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        [SerializeField] private Color _frameSelected = new Color(1f, 0.85f, 0.25f, 1f);

        private int _index;

        private void Awake()
        {
            if (_portraitButtons != null)
            {
                for (int i = 0; i < _portraitButtons.Length; i++)
                {
                    int idx = i;
                    if (_portraitButtons[i] != null) _portraitButtons[i].onClick.AddListener(() => Confirm(idx));
                }
            }
        }

        private void Start()
        {
            Highlight(0, silent: true);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _roster == null || _roster.Length == 0) return;

            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
                Highlight((_index + 1) % _roster.Length);
            else if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
                Highlight((_index - 1 + _roster.Length) % _roster.Length);
            else if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                Confirm(_index);
            else if (kb.escapeKey.wasPressedThisFrame)
                SceneFlow.Load(SceneFlow.MainMenu);
        }

        private void Highlight(int index, bool silent = false)
        {
            if (_roster == null || _roster.Length == 0) return;
            _index = Mathf.Clamp(index, 0, _roster.Length - 1);

            if (_portraitFrames != null)
                for (int i = 0; i < _portraitFrames.Length; i++)
                    if (_portraitFrames[i] != null)
                        _portraitFrames[i].color = i == _index ? _frameSelected : _frameIdle;

            if (_nameLabel != null && _roster[_index] != null)
                _nameLabel.text = _roster[_index].displayName;

            if (!silent) AudioManager.Play(SfxId.UiMove);
        }

        public void Confirm(int index)
        {
            if (_roster == null || _roster.Length == 0) return;
            index = Mathf.Clamp(index, 0, _roster.Length - 1);

            var session = GameSession.GetOrCreate();
            session.P1Character = _roster[index];
            session.P2Character = _roster[Random.Range(0, _roster.Length)];
            SceneFlow.Load(SceneFlow.Fight);
        }
    }
}
