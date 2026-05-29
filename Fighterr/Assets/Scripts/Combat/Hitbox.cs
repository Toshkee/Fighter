using System.Collections.Generic;
using UnityEngine;
using SamuraiFighter.Characters;
using SamuraiFighter.Managers;
using SamuraiFighter.Utils;

namespace SamuraiFighter.Combat
{
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] private Fighter _owner;
        [SerializeField] private Vector2 _size = new Vector2(1.5f, 1f);
        [SerializeField] private LayerMask _hurtboxLayer;
        [SerializeField] private bool _heavyImpact;

        private int _activeFrames;
        private int _damage;
        private float _knockback;
        private int _hitstopFrames;
        private readonly HashSet<Hurtbox> _alreadyHit = new();

        public bool IsActive => _activeFrames > 0;

        public void SetSize(Vector2 size) => _size = size;

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
                    Vfx.Parry(center);
                    AudioManager.Play(SfxId.Parry);
                    if (_owner != null) _owner.ApplyParryStun();
                    _activeFrames = 0;
                    return;
                }

                bool blocked = hurtbox.Owner != null && hurtbox.Owner.IsBlocking;
                int dmg = _damage;
                if (blocked)
                    dmg = Mathf.Max(0, Mathf.RoundToInt(_damage * hurtbox.Owner.BlockDamageMultiplier));
                if (hurtbox.Health != null && dmg > 0) hurtbox.Health.TakeDamage(dmg);
                if (dmg > 0 && _owner != null) _owner.NotifyHitLanded();
                ApplyKnockback(hurtbox.Owner, facing);

                if (blocked)
                {
                    HitstopController.Apply(Mathf.Max(2, _hitstopFrames / 2));
                    CameraShake.Shake(0.06f, 0.05f);
                    Vfx.Block(center, facing);
                    AudioManager.Play(SfxId.Block);
                    FloatingText.Label(center, "GUARD", new Color(0.6f, 0.85f, 1f), 0.1f);
                }
                else
                {
                    HitstopController.Apply(_hitstopFrames);
                    CameraShake.Shake(0.12f, 0.08f + 0.004f * _damage);
                    if (_heavyImpact && BattleCamera.Instance != null) BattleCamera.Instance.AddPunch(0.9f);
                    float intensity = _heavyImpact ? 1.3f : 0.85f;
                    Vfx.Hit(center, facing, intensity);
                    FloatingText.Damage(center, _damage, _heavyImpact);
                    int comboStep = _owner != null ? _owner.ComboStep : 0;
                    AudioManager.PlayHit(_heavyImpact, comboStep);
                }
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
