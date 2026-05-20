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
        Block,
        BlockStun,
        KnockDown,
        Dead,
    }

    public enum AttackKind { None, Light, Heavy, Fireball }

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
        [SerializeField] private int _lightStartup = 5;
        [SerializeField] private int _lightActive = 3;
        [SerializeField] private int _lightRecovery = 10;
        [SerializeField] private int _lightDamage = 8;
        [SerializeField] private float _lightKnockback = 7f;
        [SerializeField] private int _lightHitstopFrames = 7;

        [Header("Heavy Attack")]
        [SerializeField] private Hitbox _heavyAttackHitbox;
        [SerializeField] private int _heavyStartup = 14;
        [SerializeField] private int _heavyActive = 4;
        [SerializeField] private int _heavyRecovery = 22;
        [SerializeField] private int _heavyDamage = 18;
        [SerializeField] private float _heavyKnockback = 13f;
        [SerializeField] private int _heavyHitstopFrames = 12;

        [Header("Fireball")]
        [SerializeField] private GameObject _fireballPrefab;
        [SerializeField] private int _fireballStartup = 12;
        [SerializeField] private int _fireballActive = 2;
        [SerializeField] private int _fireballRecovery = 20;

        [Header("Hit Reaction")]
        [SerializeField] private int _hitstunFrames = 18;

        [Header("Block")]
        [SerializeField, Range(0f, 1f)] private float _blockDamageMultiplier = 0.25f;

        [Header("References")]
        [SerializeField] private Health _health;

        private Rigidbody2D _rb;
        private FighterState _currentState = FighterState.Idle;
        private float _moveInput;
        private bool _jumpPressed;
        private bool _crouchHeld;
        private bool _blockHeld;
        private bool _isGrounded;

        private int _attackFrame;
        private int _attackTotal;
        private bool _hitboxFired;

        private AttackKind _currentAttackKind = AttackKind.None;

        public FighterState CurrentState => _currentState;
        public AttackKind CurrentAttackKind => _currentAttackKind;
        public bool FacingRight => _facingRight;
        public bool IsGrounded => _isGrounded;
        public bool IsAttacking => _currentState == FighterState.Attack;
        public bool IsHit => _currentState == FighterState.Hit;
        public bool IsBlocking => _currentState == FighterState.Block;
        public bool IsDead => _currentState == FighterState.Dead;
        public float BlockDamageMultiplier => _blockDamageMultiplier;

        private int _hitFrame;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_health == null) _health = GetComponent<Health>();
            if (_health != null)
            {
                _health.OnDied += OnDied;
                _health.OnDamaged += OnDamaged;
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDied -= OnDied;
                _health.OnDamaged -= OnDamaged;
            }
        }

        private void OnDamaged(int _)
        {
            if (IsDead) return;
            if (IsBlocking) return;
            _currentState = FighterState.Hit;
            _currentAttackKind = AttackKind.None;
            _hitFrame = 0;
        }

        private void OnDied()
        {
            _currentState = FighterState.Dead;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.3f, _rb.linearVelocity.y);
            transform.rotation = Quaternion.Euler(0f, 0f, _facingRight ? -75f : 75f);
        }

        public void SetMoveInput(float horizontal) => _moveInput = horizontal;
        public void SetJumpInput(bool pressed) => _jumpPressed = pressed;
        public void SetCrouchInput(bool held) => _crouchHeld = held;
        public void SetBlockInput(bool held) => _blockHeld = held;

        private Hitbox _activeHitbox;
        private int _activeStartup;
        private int _activeActiveFrames;
        private int _activeDamage;
        private float _activeKnockback;
        private int _activeHitstop;

        public void TryLightAttack()
        {
            StartAttack(AttackKind.Light, _lightAttackHitbox, _lightStartup, _lightActive, _lightRecovery,
                        _lightDamage, _lightKnockback, _lightHitstopFrames);
        }

        public void TryHeavyAttack()
        {
            var hb = _heavyAttackHitbox != null ? _heavyAttackHitbox : _lightAttackHitbox;
            StartAttack(AttackKind.Heavy, hb, _heavyStartup, _heavyActive, _heavyRecovery,
                        _heavyDamage, _heavyKnockback, _heavyHitstopFrames);
        }

        public void TryFireball()
        {
            if (IsDead || IsAttacking || IsHit || IsBlocking || !_isGrounded || _fireballPrefab == null) return;
            _currentState = FighterState.Attack;
            _currentAttackKind = AttackKind.Fireball;
            _attackFrame = 0;
            _attackTotal = _fireballStartup + _fireballActive + _fireballRecovery;
            _hitboxFired = false;
            _activeHitbox = null;
            _activeStartup = _fireballStartup;
            _activeActiveFrames = _fireballActive;
        }

        private void SpawnFireball()
        {
            var go = Instantiate(_fireballPrefab, transform.position + new Vector3(_facingRight ? 0.6f : -0.6f, 0.1f, 0f), Quaternion.identity);
            var proj = go.GetComponent<Projectile>();
            if (proj != null) proj.Launch(this, _facingRight ? 1f : -1f);
        }

        private void StartAttack(AttackKind kind, Hitbox hb, int startup, int active, int recovery, int damage, float knockback, int hitstop)
        {
            if (IsDead || IsAttacking || IsHit || IsBlocking || !_isGrounded) return;
            _currentState = FighterState.Attack;
            _currentAttackKind = kind;
            _attackFrame = 0;
            _attackTotal = startup + active + recovery;
            _hitboxFired = false;
            _activeHitbox = hb;
            _activeStartup = startup;
            _activeActiveFrames = active;
            _activeDamage = damage;
            _activeKnockback = knockback;
            _activeHitstop = hitstop;
        }

        private void FixedUpdate()
        {
            if (IsDead) return;

            _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);

            if (IsHit)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.85f, _rb.linearVelocity.y);
                _hitFrame++;
                if (_hitFrame >= _hitstunFrames) _currentState = FighterState.Idle;
                return;
            }

            if (IsAttacking)
            {
                HandleHorizontal();
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
            if (_attackFrame == _activeStartup && !_hitboxFired)
            {
                _hitboxFired = true;
                if (_currentAttackKind == AttackKind.Fireball)
                {
                    SpawnFireball();
                }
                else if (_activeHitbox != null)
                {
                    _activeHitbox.Activate(_activeActiveFrames, _activeDamage, _activeKnockback, _activeHitstop);
                }
            }

            _attackFrame++;
            if (_attackFrame >= _attackTotal)
            {
                _currentState = FighterState.Idle;
                _currentAttackKind = AttackKind.None;
            }
        }

        private void HandleHorizontal()
        {
            if ((_crouchHeld || _blockHeld) && _isGrounded)
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
            if (_blockHeld) { _currentState = FighterState.Block; return; }
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
