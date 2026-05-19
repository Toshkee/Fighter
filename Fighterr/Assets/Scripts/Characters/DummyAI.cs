using UnityEngine;

namespace SamuraiFighter.Characters
{
    [RequireComponent(typeof(Fighter))]
    public class DummyAI : MonoBehaviour
    {
        [SerializeField] private Fighter _self;
        [SerializeField] private Fighter _target;
        [SerializeField] private float _attackRange = 1.4f;
        [SerializeField] private float _approachDeadzone = 0.2f;
        [SerializeField] private float _attackCooldown = 0.9f;
        [SerializeField] private float _reactionDelay = 0.25f;

        private float _cooldownTimer;
        private float _reactionTimer;

        private void Awake()
        {
            if (_self == null) _self = GetComponent<Fighter>();
        }

        private void Update()
        {
            if (_self == null || _self.IsDead) { Stop(); return; }
            if (_target == null || _target.IsDead) { Stop(); return; }

            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
            if (_reactionTimer > 0f) _reactionTimer -= Time.deltaTime;

            if (_self.IsAttacking) { _self.SetMoveInput(0f); return; }

            float dx = _target.transform.position.x - _self.transform.position.x;
            float dist = Mathf.Abs(dx);

            if (dist <= _attackRange)
            {
                _self.SetMoveInput(0f);
                if (_cooldownTimer <= 0f && _reactionTimer <= 0f && _self.IsGrounded)
                {
                    _self.TryLightAttack();
                    _cooldownTimer = _attackCooldown;
                    _reactionTimer = _reactionDelay;
                }
            }
            else if (dist > _attackRange + _approachDeadzone)
            {
                _self.SetMoveInput(Mathf.Sign(dx));
            }
            else
            {
                _self.SetMoveInput(0f);
            }
        }

        private void Stop()
        {
            if (_self != null) _self.SetMoveInput(0f);
        }

        public void SetTarget(Fighter target) { _target = target; }
    }
}
