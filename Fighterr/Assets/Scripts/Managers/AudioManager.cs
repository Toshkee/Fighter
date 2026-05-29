using System.Collections.Generic;
using UnityEngine;
using SamuraiFighter.Utils;

namespace SamuraiFighter.Managers
{
    /// <summary>
    /// Plays procedurally-generated SFX through a small round-robin pool of
    /// <see cref="AudioSource"/>s and drives a looping music bed. Self-bootstraps
    /// the same way as <c>HitstopController</c>/<c>CameraShake</c> — call the static
    /// API from anywhere and it creates a persistent instance on first use.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const int PoolSize = 12;

        private static AudioManager _instance;
        private static bool _appQuitting;

        private Dictionary<SfxId, AudioClip> _clips;
        private AudioSource[] _pool;
        private int _next;
        private AudioSource _music;

        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.75f;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.25f;
        [Tooltip("Music is opt-in — nothing auto-plays it. Call AudioManager.PlayMusic() to start.")]
        [SerializeField] private bool _musicEnabled = false;

        public static AudioManager Instance
        {
            get { EnsureInstance(); return _instance; }
        }

        private static void EnsureInstance()
        {
            if (_instance != null || _appQuitting) return;
            var go = new GameObject("AudioManager");
            _instance = go.AddComponent<AudioManager>();
            DontDestroyOnLoad(go);
            _instance.Init();
        }

        private void Init()
        {
            _clips = ProceduralSfx.BuildAll();

            _pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f; // 2D
                _pool[i] = src;
            }

            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;
            _music.volume = _musicVolume;
            // Music clip is built lazily on first PlayMusic() so startup stays cheap when music is off.
        }

        private void OnApplicationQuit() => _appQuitting = true;

        // ---- static convenience API ----

        public static void Play(SfxId id, float volume = 1f, float pitchVar = 0.07f, float pitchOffset = 0f)
        {
            EnsureInstance();
            _instance?.PlayInternal(id, volume, pitchVar, pitchOffset);
        }

        /// <summary>Impact sound; pitch rises with the current combo step for a rising-tension feel.</summary>
        public static void PlayHit(bool heavy, int comboStep)
        {
            EnsureInstance();
            if (_instance == null) return;
            float pitch = Mathf.Min(0.3f, comboStep * 0.05f);
            _instance.PlayInternal(heavy ? SfxId.HitHeavy : SfxId.HitLight, heavy ? 0.85f : 0.7f, 0.04f, pitch);
        }

        /// <summary>Builds all clips ahead of time so the first hit doesn't hitch. No music.</summary>
        public static void Prewarm() => EnsureInstance();

        public static void PlayMusic(bool force = false)
        {
            EnsureInstance();
            if (_instance?._music == null) return;
            if (!force && !_instance._musicEnabled) return;
            if (_instance._music.clip == null) _instance._music.clip = ProceduralSfx.BuildMusicLoop();
            if (!_instance._music.isPlaying) _instance._music.Play();
        }

        public static void StopMusic()
        {
            if (_instance?._music != null) _instance._music.Stop();
        }

        private void PlayInternal(SfxId id, float volume, float pitchVar, float pitchOffset)
        {
            if (_clips == null || !_clips.TryGetValue(id, out var clip) || clip == null) return;
            var src = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            float jitter = pitchVar > 0f ? Random.Range(-pitchVar, pitchVar) : 0f;
            src.pitch = Mathf.Clamp(1f + pitchOffset + jitter, 0.4f, 2.5f);
            src.PlayOneShot(clip, Mathf.Clamp01(volume) * _sfxVolume);
        }
    }
}
