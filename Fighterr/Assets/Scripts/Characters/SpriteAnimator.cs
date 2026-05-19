using System.Collections.Generic;
using UnityEngine;

namespace SamuraiFighter.Characters
{
    public class SpriteAnimator : MonoBehaviour
    {
        [System.Serializable]
        public class Clip
        {
            public FighterState state;
            public AttackKind attackKind = AttackKind.None;
            public Sprite[] frames;
            public float fps = 10f;
            public bool loop = true;
        }

        [SerializeField] private Fighter _fighter;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private List<Clip> _clips = new List<Clip>();

        private Clip _current;
        private float _timer;
        private int _frame;

        public void SetClips(List<Clip> clips) { _clips = clips; }

        private void Awake()
        {
            if (_fighter == null) _fighter = GetComponent<Fighter>();
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_fighter == null || _renderer == null) return;

            Clip clip = null;
            if (_fighter.CurrentState == FighterState.Attack)
            {
                clip = FindAttackClip(_fighter.CurrentAttackKind);
            }
            if (clip == null)
            {
                var target = MapState(_fighter.CurrentState);
                clip = FindClip(target);
            }
            if (clip == null) clip = FindClip(FighterState.Idle);
            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

            if (clip != _current)
            {
                _current = clip;
                _frame = 0;
                _timer = 0f;
                _renderer.sprite = clip.frames[0];
                return;
            }

            _timer += Time.deltaTime;
            float frameDur = 1f / Mathf.Max(0.1f, clip.fps);
            while (_timer >= frameDur)
            {
                _timer -= frameDur;
                _frame++;
                if (_frame >= clip.frames.Length)
                {
                    _frame = clip.loop ? 0 : clip.frames.Length - 1;
                }
            }
            _renderer.sprite = clip.frames[_frame];
        }

        private static FighterState MapState(FighterState s)
        {
            switch (s)
            {
                case FighterState.Attack:
                case FighterState.AirAttack:
                case FighterState.Hit:
                case FighterState.BlockStun:
                case FighterState.KnockDown:
                case FighterState.Crouch:
                case FighterState.Dead:
                    return FighterState.Idle;
                default:
                    return s;
            }
        }

        private Clip FindClip(FighterState s)
        {
            for (int i = 0; i < _clips.Count; i++)
                if (_clips[i].state == s && _clips[i].attackKind == AttackKind.None) return _clips[i];
            return null;
        }

        private Clip FindAttackClip(AttackKind kind)
        {
            for (int i = 0; i < _clips.Count; i++)
                if (_clips[i].state == FighterState.Attack && _clips[i].attackKind == kind) return _clips[i];
            return null;
        }
    }
}
