using UnityEngine;
using SamuraiFighter.Combat;

namespace SamuraiFighter.Characters
{
    public enum FighterState
    {
        Idle,
        Walk,
        Jump,
        Crouch,
        Attack,
        AirAttack,
        Hit,
        BlockStun,
        KnockDown,
        Dead,
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class Fighter : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 5f;
        [SerializeField] private float _jumpForce = 12f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float _groundCheckRadius = 0.1f;

        [Header("Facing")]
        [SerializeField] private bool _facingRight = true;

        [Header("Light Attack")]
        [SerializeField] private Hitbox _lightAttackHitbox;
        [SerializeField] private int _lightStartup = 6;
        [SerializeField] private int _lightActive = 3;
        [SerializeField] private int _lightRecovery = 12;
        [SerializeField] private int _lightDamage = 8;
        [SerializeField] private float _lightKnockback = 6f;
        [SerializeField] private int _lightHitstopFrames = 6;

        private Rigidbody2D _rb;
        private FighterState _currentState = FighterState.Idle;
        private float _moveInput;
        private bool _jumpPressed;
        private bool _crouchHeld;
        private bool _isGrounded;

        private int _attackFrame;
        private int _attackTotal;
        private bool _hitboxFired;

        public FighterState CurrentState => _currentState;
        public bool FacingRight => _facingRight;
        public bool IsGrounded => _isGrounded;
        public bool IsAttacking => _currentState == FighterState.Attack;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public void SetMoveInput(float horizontal) => _moveInput = horizontal;
        public void SetJumpInput(bool pressed) => _jumpPressed = pressed;
        public void SetCrouchInput(bool held) => _crouchHeld = held;

        public void TryLightAttack()
        {
            if (IsAttacking || !_isGrounded) return;
            _currentState = FighterState.Attack;
            _attackFrame = 0;
            _attackTotal = _lightStartup + _lightActive + _lightRecovery;
            _hitboxFired = false;
        }

        private void FixedUpdate()
        {
            _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);

            if (IsAttacking)
            {
                TickAttack();
            }
            else
            {
                HandleHorizontal();
                HandleJump();
                UpdateState();
                UpdateFacing();
            }
        }

        private void TickAttack()
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

            if (_attackFrame == _lightStartup && !_hitboxFired)
            {
                _hitboxFired = true;
                if (_lightAttackHitbox != null)
                {
                    _lightAttackHitbox.Activate(_lightActive, _lightDamage, _lightKnockback, _lightHitstopFrames);
                }
            }

            _attackFrame++;
            if (_attackFrame >= _attackTotal)
            {
                _currentState = FighterState.Idle;
            }
        }

        private void HandleHorizontal()
        {
            if (_crouchHeld && _isGrounded)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                return;
            }
            _rb.linearVelocity = new Vector2(_moveInput * _walkSpeed, _rb.linearVelocity.y);
        }

        private void HandleJump()
        {
            if (_jumpPressed && _isGrounded && !_crouchHeld)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
            }
            _jumpPressed = false;
        }

        private void UpdateState()
        {
            if (!_isGrounded) { _currentState = FighterState.Jump; return; }
            if (_crouchHeld) { _currentState = FighterState.Crouch; return; }
            _currentState = Mathf.Abs(_moveInput) > 0.01f ? FighterState.Walk : FighterState.Idle;
        }

        private void UpdateFacing()
        {
            if (_moveInput > 0.01f && !_facingRight) Flip();
            else if (_moveInput < -0.01f && _facingRight) Flip();
        }

        private void Flip()
        {
            _facingRight = !_facingRight;
            Vector3 s = transform.localScale;
            s.x *= -1f;
            transform.localScale = s;
        }

        private void OnDrawGizmosSelected()
        {
            if (_groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
        }
    }
}
