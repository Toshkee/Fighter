using UnityEngine;
using SamuraiFighter.Characters;

namespace SamuraiFighter.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 9f;
        [SerializeField] private int _damage = 12;
        [SerializeField] private float _knockback = 8f;
        [SerializeField] private int _hitstopFrames = 8;
        [SerializeField] private float _lifetime = 2.5f;

        private Fighter _owner;
        private Rigidbody2D _rb;
        private float _direction;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
        }

        public void Launch(Fighter owner, float direction)
        {
            _owner = owner;
            _direction = Mathf.Sign(direction);
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * _direction;
            transform.localScale = s;
            _rb.linearVelocity = new Vector2(_speed * _direction, 0f);
            Destroy(gameObject, _lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var hurt = other.GetComponent<Hurtbox>();
            if (hurt == null) return;
            if (_owner != null && hurt.Owner == _owner) return;
            int dmg = _damage;
            if (hurt.Owner != null && hurt.Owner.IsBlocking)
                dmg = Mathf.Max(0, Mathf.RoundToInt(_damage * hurt.Owner.BlockDamageMultiplier));
            if (hurt.Health != null && dmg > 0) hurt.Health.TakeDamage(dmg);
            if (hurt.Owner != null)
            {
                var rb = hurt.Owner.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = new Vector2(_direction * _knockback, rb.linearVelocity.y);
            }
            HitstopController.Apply(_hitstopFrames);
            HitFeedback.Spawn(transform.position);
            Destroy(gameObject);
        }
    }
}
