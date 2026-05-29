using UnityEngine;

namespace SamuraiFighter.Characters
{
    /// <summary>
    /// Spawns fading sprite "afterimages" while the fighter is moving fast — dashing or
    /// throwing a heavy/super. Auto-wires to the Fighter + SpriteRenderer on the same
    /// object, so the builder only has to AddComponent it. Ghosts fade on unscaled time
    /// so they keep animating through hitstop / slow-motion.
    /// </summary>
    public class MotionTrail : MonoBehaviour
    {
        [SerializeField] private Fighter _fighter;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Color _tint = new Color(0.6f, 0.85f, 1f, 0.55f);
        [SerializeField] private float _interval = 0.035f;
        [SerializeField] private float _ghostLife = 0.26f;

        private float _timer;

        private void Awake()
        {
            if (_fighter == null) _fighter = GetComponent<Fighter>();
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_fighter == null || _renderer == null) return;

            bool fast = _fighter.IsDashing ||
                        (_fighter.IsAttacking &&
                         (_fighter.CurrentAttackKind == AttackKind.Super ||
                          _fighter.CurrentAttackKind == AttackKind.Heavy));

            if (!fast) { _timer = 0f; return; }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _interval;
                SpawnGhost();
            }
        }

        private void SpawnGhost()
        {
            if (_renderer.sprite == null) return;
            var go = new GameObject("Ghost");
            var t = _renderer.transform;
            go.transform.position = t.position;
            go.transform.rotation = t.rotation;
            go.transform.localScale = t.lossyScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _renderer.sprite;
            sr.flipX = _renderer.flipX;
            sr.color = _tint;
            sr.sortingOrder = _renderer.sortingOrder - 1;

            go.AddComponent<GhostFade>().Init(_tint, _ghostLife);
        }
    }

    public class GhostFade : MonoBehaviour
    {
        private Color _base;
        private float _life;
        private float _age;
        private SpriteRenderer _sr;

        public void Init(Color color, float life)
        {
            _base = color;
            _life = Mathf.Max(0.05f, life);
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            float t = _age / _life;
            if (t >= 1f) { Destroy(gameObject); return; }
            if (_sr != null)
            {
                var c = _base;
                c.a = _base.a * (1f - t);
                _sr.color = c;
            }
        }
    }
}
