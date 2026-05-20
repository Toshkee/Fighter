using UnityEngine;

namespace SamuraiFighter.Characters
{
    [RequireComponent(typeof(Fighter))]
    public class DummyAI : MonoBehaviour
    {
        [SerializeField] private Fighter _self;
        [SerializeField] private Fighter _target;

        [SerializeField] private float _closeRange = 1.4f;
        [SerializeField] private float _midRange = 3.5f;
        [SerializeField] private float _fireballRange = 6.0f;
        [SerializeField] private float _approachDeadzone = 0.2f;

        [SerializeField] private float _attackCooldown = 0.7f;
        [SerializeField] private float _fireballCooldown = 2.0f;
        [SerializeField] private float _decisionInterval = 0.25f;
        [SerializeField] private float _reactionDelay = 0.18f;

        [SerializeField] [Range(0f, 1f)] private float _blockChance = 0.45f;
        [SerializeField] [Range(0f, 1f)] private float _heavyChance = 0.35f;
        [SerializeField] [Range(0f, 1f)] private float _retreatChance = 0.2f;
        [SerializeField] [Range(0f, 1f)] private float _jumpInChance = 0.15f;

        private float _attackTimer;
        private float _fireballTimer;
        private float _decisionTimer;
        private float _reactionTimer;
        private float _retreatTimer;
        private bool _blockingNow;
        private bool _prevTargetAttacking;

        private void Awake()
        {
            if (_self == null) _self = GetComponent<Fighter>();
        }

        private void Update()
        {
            if (_self == null || _self.IsDead) { Stop(); return; }
            if (_target == null || _target.IsDead) { Stop(); return; }

            float dt = Time.deltaTime;
            if (_attackTimer > 0f) _attackTimer -= dt;
            if (_fireballTimer > 0f) _fireballTimer -= dt;
            if (_decisionTimer > 0f) _decisionTimer -= dt;
            if (_reactionTimer > 0f) _reactionTimer -= dt;
            if (_retreatTimer > 0f) _retreatTimer -= dt;

            // Drop block from previous frame; we re-decide each frame.
            if (_blockingNow) { _self.SetBlockInput(false); _blockingNow = false; }

            if (_self.IsAttacking || _self.IsHit) { _self.SetMoveInput(0f); return; }

            float dx = _target.transform.position.x - _self.transform.position.x;
            float dist = Mathf.Abs(dx);
            float dir = Mathf.Sign(dx);

            // React to target starting an attack: maybe block.
            bool targetJustAttacked = _target.IsAttacking && !_prevTargetAttacking;
            _prevTargetAttacking = _target.IsAttacking;

            if (targetJustAttacked && dist <= _closeRange + 0.5f && Random.value < _blockChance)
            {
                _reactionTimer = _reactionDelay;
                _retreatTimer = 0.35f;
            }

            if (_reactionTimer > 0f && _target.IsAttacking && dist <= _closeRange + 0.8f)
            {
                _self.SetMoveInput(0f);
                _self.SetBlockInput(true);
                _blockingNow = true;
                return;
            }

            // Periodic high-level decision: retreat / hold.
            if (_decisionTimer <= 0f)
            {
                _decisionTimer = _decisionInterval + Random.Range(-0.1f, 0.15f);
                if (dist < _closeRange && Random.value < _retreatChance)
                    _retreatTimer = Random.Range(0.3f, 0.6f);
            }

            // Retreating: walk away from target.
            if (_retreatTimer > 0f)
            {
                _self.SetMoveInput(-dir);
                return;
            }

            // Close range: attack with light/heavy/super mix.
            if (dist <= _closeRange)
            {
                _self.SetMoveInput(0f);
                if (_attackTimer <= 0f && _self.IsGrounded)
                {
                    var meter = _self.SuperMeter;
                    if (meter != null && meter.IsFull && Random.value < 0.6f && _self.TrySuper())
                    {
                        _attackTimer = _attackCooldown + 0.5f;
                    }
                    else
                    {
                        if (Random.value < _heavyChance) _self.TryHeavyAttack();
                        else _self.TryLightAttack();
                        _attackTimer = _attackCooldown + Random.Range(-0.1f, 0.2f);
                    }
                }
                return;
            }

            // Mid range: close in, sometimes jump-in.
            if (dist <= _midRange)
            {
                _self.SetMoveInput(dir);
                if (_self.IsGrounded && Random.value < _jumpInChance * Time.deltaTime * 4f)
                    _self.SetJumpInput(true);
                return;
            }

            // Long range: fireball if ready, else walk forward.
            if (dist <= _fireballRange && _fireballTimer <= 0f && _self.IsGrounded)
            {
                _self.SetMoveInput(0f);
                _self.TryFireball();
                _fireballTimer = _fireballCooldown + Random.Range(-0.3f, 0.5f);
                return;
            }

            if (dist > _closeRange + _approachDeadzone)
                _self.SetMoveInput(dir);
            else
                _self.SetMoveInput(0f);
        }

        private void Stop()
        {
            if (_self == null) return;
            _self.SetMoveInput(0f);
            _self.SetBlockInput(false);
        }

        public void SetTarget(Fighter target) { _target = target; }
    }
}
