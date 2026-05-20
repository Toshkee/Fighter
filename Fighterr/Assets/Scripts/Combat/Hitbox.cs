using System.Collections.Generic;
using UnityEngine;
using SamuraiFighter.Characters;

namespace SamuraiFighter.Combat
{
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] private Fighter _owner;
        [SerializeField] private Vector2 _size = new Vector2(1.5f, 1f);
        [SerializeField] private LayerMask _hurtboxLayer;

        private int _activeFrames;
        private int _damage;
        private float _knockback;
        private int _hitstopFrames;
        private readonly HashSet<Hurtbox> _alreadyHit = new();

        public bool IsActive => _activeFrames > 0;

        public void Activate(int frames, int damage, float knockback, int hitstopFrames)
        {
            _activeFrames = frames;
            _damage = damage;
            _knockback = knockback;
            _hitstopFrames = hitstopFrames;
            _alreadyHit.Clear();
        }

        private void FixedUpdate()
        {
            if (_activeFrames <= 0) return;

            int facing = _owner != null && _owner.FacingRight ? 1 : -1;
            Vector2 center = (Vector2)transform.position + new Vector2(facing * _size.x * 0.5f, 0f);
            var hits = Physics2D.OverlapBoxAll(center, _size, 0f, _hurtboxLayer);

            foreach (var col in hits)
            {
                var hurtbox = col.GetComponent<Hurtbox>();
                if (hurtbox == null) continue;
                if (hurtbox.Owner == _owner) continue;
                if (_alreadyHit.Contains(hurtbox)) continue;
                if (hurtbox.Owner != null && hurtbox.Owner.IsInvulnerable) continue;
                _alreadyHit.Add(hurtbox);

                if (hurtbox.Owner != null && hurtbox.Owner.IsParrying)
                {
                    HitstopController.Apply(Mathf.Max(_hitstopFrames * 2, 18));
                    CameraShake.Shake(0.25f, 0.18f);
                    HitFeedback.Spawn(center);
                    if (_owner != null) _owner.ApplyParryStun();
                    _activeFrames = 0;
                    return;
                }

                int dmg = _damage;
                if (hurtbox.Owner != null && hurtbox.Owner.IsBlocking)
                    dmg = Mathf.Max(0, Mathf.RoundToInt(_damage * hurtbox.Owner.BlockDamageMultiplier));
                if (hurtbox.Health != null && dmg > 0) hurtbox.Health.TakeDamage(dmg);
                if (dmg > 0 && _owner != null) _owner.NotifyHitLanded();
                ApplyKnockback(hurtbox.Owner, facing);
                HitstopController.Apply(_hitstopFrames);
                CameraShake.Shake(0.12f, 0.08f + 0.004f * _damage);
                HitFeedback.Spawn(center);
            }

            _activeFrames--;
        }

        private void ApplyKnockback(Fighter target, int facing)
        {
            if (target == null) return;
            var rb = target.GetComponent<Rigidbody2D>();
            if (rb == null) return;
            rb.linearVelocity = new Vector2(facing * _knockback, rb.linearVelocity.y);
        }

        private void OnDrawGizmosSelected()
        {
            int facing = _owner != null && _owner.FacingRight ? 1 : -1;
            Vector3 center = transform.position + new Vector3(facing * _size.x * 0.5f, 0f, 0f);
            Gizmos.color = IsActive ? Color.red : new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireCube(center, _size);
        }
    }
}
