using UnityEngine;
using UnityEngine.InputSystem;
using SamuraiFighter.Characters;

namespace SamuraiFighter.Input
{
    [RequireComponent(typeof(Fighter))]
    public class PlayerInputHandler : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private float _doubleTapWindow = 0.25f;

        private Fighter _fighter;
        private InputActionMap _map;
        private InputAction _move;
        private InputAction _jump;
        private InputAction _attack;
        private InputAction _heavy;

        private float _lastLeftTap = -10f;
        private float _lastRightTap = -10f;
        private bool _leftWasDown;
        private bool _rightWasDown;

        private void Awake()
        {
            _fighter = GetComponent<Fighter>();
        }

        private void OnEnable()
        {
            if (_actions == null)
            {
                Debug.LogError("PlayerInputHandler: InputActionAsset not assigned.");
                return;
            }
            _map = _actions.FindActionMap("Player", throwIfNotFound: true);
            _move = _map.FindAction("Move", throwIfNotFound: true);
            _jump = _map.FindAction("Jump", throwIfNotFound: true);
            _attack = _map.FindAction("Attack", throwIfNotFound: true);
            _heavy = _map.FindAction("Interact", throwIfNotFound: true);
            _jump.performed += OnJumpPerformed;
            _attack.performed += OnAttackPerformed;
            _heavy.performed += OnHeavyPerformed;
            _map.Enable();
        }

        private void OnDisable()
        {
            if (_jump != null) _jump.performed -= OnJumpPerformed;
            if (_attack != null) _attack.performed -= OnAttackPerformed;
            if (_heavy != null) _heavy.performed -= OnHeavyPerformed;
            if (_map != null) _map.Disable();
        }

        private void Update()
        {
            if (_move == null) return;
            Vector2 v = _move.ReadValue<Vector2>();
            _fighter.SetMoveInput(v.x);
            _fighter.SetCrouchInput(v.y < -0.5f);

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.fKey.wasPressedThisFrame) _fighter.TryFireball();
                if (kb.qKey.wasPressedThisFrame) _fighter.TrySuper();
                _fighter.SetBlockInput(kb.gKey.isPressed);

                bool leftDown = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
                bool rightDown = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                if (leftDown && !_leftWasDown)
                {
                    if (Time.time - _lastLeftTap <= _doubleTapWindow) _fighter.TryDash(-1f);
                    _lastLeftTap = Time.time;
                }
                if (rightDown && !_rightWasDown)
                {
                    if (Time.time - _lastRightTap <= _doubleTapWindow) _fighter.TryDash(1f);
                    _lastRightTap = Time.time;
                }
                _leftWasDown = leftDown;
                _rightWasDown = rightDown;

                if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame)
                    _fighter.TryDash(v.x);
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext _)
        {
            _fighter.SetJumpInput(true);
        }

        private void OnAttackPerformed(InputAction.CallbackContext _)
        {
            _fighter.TryLightAttack();
        }

        private void OnHeavyPerformed(InputAction.CallbackContext _)
        {
            _fighter.TryHeavyAttack();
        }
    }
}
