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

    public enum AttackKind { None, Light, Heavy, Fireball, Super }

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

        [Header("Hitbox slots (scene refs)")]
        [SerializeField] private Hitbox _lightAttackHitbox;
        [SerializeField] private Hitbox _heavyAttackHitbox;

        [Header("Moves (ScriptableObjects)")]
        [SerializeField] private MoveData _lightMove;
        [SerializeField] private MoveData _heavyMove;
        [SerializeField] private MoveData _superMove;
        [SerializeField] private MoveData _fireballMove;

        [Header("Hit Reaction")]
        [SerializeField] private int _hitstunFrames = 18;

        [Header("Dash")]
        [SerializeField] private float _dashSpeed = 14f;
        [SerializeField] private int _dashFrames = 10;
        [SerializeField] private int _dashIFrames = 14;
        [SerializeField] private int _dashCooldownFrames = 30;

        [Header("Combo")]
        [SerializeField] private int _comboWindowFrames = 22;

        [Header("Block")]
        [SerializeField, Range(0f, 1f)] private float _blockDamageMultiplier = 0.25f;
        [SerializeField] private int _parryWindowFrames = 8;
        [SerializeField] private int _parryStunFrames = 30;
        [SerializeField] private int _parryCooldownFrames = 45;

        [Header("Super meter gain")]
        [SerializeField] private int _meterGainOnHitLanded = 35;
        [SerializeField] private int _meterGainOnHitTaken = 22;

        [Header("References")]
        [SerializeField] private Health _health;
        [SerializeField] private HitFlash _hitFlash;
        [SerializeField] private SuperMeter _superMeter;

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
        private int _parryWindow;
        private int _parryCooldown;
        private bool _prevBlockHeld;

        private int _dashFramesLeft;
        private int _iFrames;
        private int _dashCooldown;
        private float _dashDir;

        private int _comboWindow;
        private int _comboStep;

        public FighterState CurrentState => _currentState;
        public AttackKind CurrentAttackKind => _currentAttackKind;
        public bool FacingRight => _facingRight;
        public bool IsGrounded => _isGrounded;
        public bool IsAttacking => _currentState == FighterState.Attack;
        public bool IsHit => _currentState == FighterState.Hit;
        public bool IsBlocking => _currentState == FighterState.Block;
        public bool IsDead => _currentState == FighterState.Dead;
        public bool IsParrying => _parryWindow > 0 && _currentState == FighterState.Block;
        public bool IsInvulnerable => _iFrames > 0 || IsDead;
        public bool IsDashing => _dashFramesLeft > 0;
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
            if (_superMeter != null) _superMeter.Add(_meterGainOnHitTaken);
            if (IsDead) return;
            if (IsBlocking) return;
            _currentState = FighterState.Hit;
            _currentAttackKind = AttackKind.None;
            _hitFrame = 0;
            if (_hitFlash != null) _hitFlash.Flash();
        }

        public void NotifyHitLanded()
        {
            if (_superMeter != null) _superMeter.Add(_meterGainOnHitLanded);
        }

        public SuperMeter SuperMeter => _superMeter;

        public void ApplyParryStun()
        {
            if (IsDead) return;
            _currentState = FighterState.Hit;
            _currentAttackKind = AttackKind.None;
            _hitFrame = Mathf.Max(0, _hitstunFrames - _parryStunFrames);
            if (_activeHitbox != null) _activeHitbox.Activate(0, 0, 0f, 0);
            if (_hitFlash != null) _hitFlash.Flash();
        }

        private void OnDied()
        {
            _currentState = FighterState.Dead;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.3f, _rb.linearVelocity.y);
            transform.rotation = Quaternion.Euler(0f, 0f, _facingRight ? -75f : 75f);
        }

        public void ResetFighter(Vector3 position, bool facingRight)
        {
            _currentState = FighterState.Idle;
            _currentAttackKind = AttackKind.None;
            _facingRight = facingRight;
            _moveInput = 0f;
            _jumpPressed = false;
            _crouchHeld = false;
            _blockHeld = false;
            _prevBlockHeld = false;
            _attackFrame = 0;
            _attackTotal = 0;
            _hitboxFired = false;
            _hitFrame = 0;
            _parryWindow = 0;
            _parryCooldown = 0;
            _iFrames = 0;
            _dashFramesLeft = 0;
            _dashCooldown = 0;
            _comboWindow = 0;
            _comboStep = 0;
            if (_activeHitbox != null) _activeHitbox.Activate(0, 0, 0f, 0);
            _activeHitbox = null;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            transform.localScale = new Vector3(facingRight ? 1f : -1f, 1f, 1f);
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            if (_health != null) _health.ResetHealth();
        }

        public void AssignMoves(MoveData light, MoveData heavy, MoveData super, MoveData fireball)
        {
            _lightMove = light;
            _heavyMove = heavy;
            _superMove = super;
            _fireballMove = fireball;
        }

        public void SetMoveInput(float horizontal) => _moveInput = horizontal;
        public void SetJumpInput(bool pressed) => _jumpPressed = pressed;
        public void SetCrouchInput(bool held) => _crouchHeld = held;
        public void SetBlockInput(bool held)
        {
            if (held && !_prevBlockHeld && _parryCooldown <= 0 && _isGrounded && !IsAttacking && !IsHit && !IsDead)
            {
                _parryWindow = _parryWindowFrames;
                _parryCooldown = _parryCooldownFrames;
            }
            _prevBlockHeld = held;
            _blockHeld = held;
        }

        private Hitbox _activeHitbox;
        private int _activeStartup;
        private int _activeActiveFrames;
        private int _activeDamage;
        private float _activeKnockback;
        private int _activeHitstop;

        public void TryLightAttack() => TryHitboxMove(_lightMove);
        public void TryHeavyAttack() => TryHitboxMove(_heavyMove);

        private void TryHitboxMove(MoveData move)
        {
            if (move == null) return;
            int dmg = move.damage;
            int startup = move.startup;
            if (_comboWindow > 0 && _comboStep >= 1 && (move.comboDamageBonus > 1f || move.comboStartupReduction > 0))
            {
                dmg = Mathf.RoundToInt(move.damage * move.comboDamageBonus);
                startup = Mathf.Max(move.comboMinStartup, move.startup - move.comboStartupReduction);
            }
            StartAttack(move.attackKind, ResolveHitbox(move.hitboxSlot), startup, move.active, move.recovery,
                        dmg, move.knockback, move.hitstopFrames);
        }

        private Hitbox ResolveHitbox(HitboxSlot slot)
        {
            switch (slot)
            {
                case HitboxSlot.Heavy: return _heavyAttackHitbox != null ? _heavyAttackHitbox : _lightAttackHitbox;
                default: return _lightAttackHitbox;
            }
        }

        public bool TryDash(float direction)
        {
            if (IsDead || IsHit || _dashCooldown > 0 || _dashFramesLeft > 0) return false;
            if (Mathf.Abs(direction) < 0.1f) direction = _facingRight ? 1f : -1f;
            _dashDir = Mathf.Sign(direction);
            _dashFramesLeft = _dashFrames;
            _iFrames = _dashIFrames;
            _dashCooldown = _dashCooldownFrames;
            if (IsAttacking)
            {
                _currentState = FighterState.Idle;
                _currentAttackKind = AttackKind.None;
                if (_activeHitbox != null) _activeHitbox.Activate(0, 0, 0f, 0);
            }
            return true;
        }

        public bool TrySuper()
        {
            if (_superMove == null) return false;
            if (IsDead || IsAttacking || IsHit || IsBlocking || !_isGrounded) return false;
            if (_superMeter == null || _superMeter.Current < _superMove.superCost) return false;
            _superMeter.Drain(_superMove.superCost);
            StartAttack(AttackKind.Super, ResolveHitbox(_superMove.hitboxSlot),
                        _superMove.startup, _superMove.active, _superMove.recovery,
                        _superMove.damage, _superMove.knockback, _superMove.hitstopFrames);
            SuperFlash.Trigger(_superMove.superFlashDuration, null);
            return true;
        }

        public void TryFireball()
        {
            if (_fireballMove == null || _fireballMove.projectilePrefab == null) return;
            if (IsDead || IsAttacking || IsHit || IsBlocking || !_isGrounded) return;
            _currentState = FighterState.Attack;
            _currentAttackKind = AttackKind.Fireball;
            _attackFrame = 0;
            _attackTotal = _fireballMove.startup + _fireballMove.active + _fireballMove.recovery;
            _hitboxFired = false;
            _activeHitbox = null;
            _activeStartup = _fireballMove.startup;
            _activeActiveFrames = _fireballMove.active;
        }

        private void SpawnFireball()
        {
            if (_fireballMove == null || _fireballMove.projectilePrefab == null) return;
            var go = Instantiate(_fireballMove.projectilePrefab, transform.position + new Vector3(_facingRight ? 0.6f : -0.6f, 0.1f, 0f), Quaternion.identity);
            var proj = go.GetComponent<Projectile>();
            if (proj != null) proj.Launch(this, _facingRight ? 1f : -1f);
        }

        private void StartAttack(AttackKind kind, Hitbox hb, int startup, int active, int recovery, int damage, float knockback, int hitstop)
        {
            if (IsDead || IsHit || IsBlocking || !_isGrounded) return;
            if (IsAttacking)
            {
                bool inRecovery = _attackFrame >= (_activeStartup + _activeActiveFrames);
                if (!(inRecovery && _comboWindow > 0)) return;
                if (_activeHitbox != null) _activeHitbox.Activate(0, 0, 0f, 0);
            }
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

            if (_parryWindow > 0) _parryWindow--;
            if (_parryCooldown > 0) _parryCooldown--;
            if (_iFrames > 0) _iFrames--;
            if (_dashCooldown > 0) _dashCooldown--;
            if (_comboWindow > 0) { _comboWindow--; if (_comboWindow == 0) _comboStep = 0; }

            if (_dashFramesLeft > 0)
            {
                _rb.linearVelocity = new Vector2(_dashDir * _dashSpeed, 0f);
                _dashFramesLeft--;
                _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);
                return;
            }

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

            if (_attackFrame == _activeStartup + _activeActiveFrames)
            {
                _comboStep = Mathf.Min(_comboStep + 1, 2);
                _comboWindow = _comboWindowFrames;
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
