using System.Collections.Generic;
using UnityEngine;

namespace SamuraiFighter.Utils
{
    /// <summary>
    /// Identifiers for every procedurally-synthesised sound the game can play.
    /// No audio files ship with the project — <see cref="ProceduralSfx"/> generates
    /// every clip from raw samples at runtime so combat always has sound.
    /// </summary>
    public enum SfxId
    {
        LightSwing,
        HeavySwing,
        Whiff,
        HitLight,
        HitHeavy,
        Block,
        Parry,
        Fireball,
        Super,
        Jump,
        Dash,
        KO,
        RoundStart,
        UiMove,
        UiConfirm,
    }

    /// <summary>
    /// Builds short SFX clips and a seamless music loop in code. Everything is mono,
    /// 44.1kHz, generated once and cached by <see cref="Managers.AudioManager"/>.
    /// </summary>
    public static class ProceduralSfx
    {
        private const int SampleRate = 44100;
        private const float Tau = 6.2831855f;

        // Single shared RNG so generation is deterministic run-to-run (single threaded).
        private static System.Random _rng = new System.Random(20240529);
        private static float Noise() => (float)(_rng.NextDouble() * 2.0 - 1.0);

        /// <summary>Generates every SFX clip. Call once at startup.</summary>
        public static Dictionary<SfxId, AudioClip> BuildAll()
        {
            _rng = new System.Random(20240529);
            var map = new Dictionary<SfxId, AudioClip>
            {
                [SfxId.LightSwing] = Make("sfx_lightSwing", 0.13f, LightSwing),
                [SfxId.HeavySwing] = Make("sfx_heavySwing", 0.20f, HeavySwing),
                [SfxId.Whiff]      = Make("sfx_whiff", 0.14f, Whiff),
                [SfxId.HitLight]   = Make("sfx_hitLight", 0.13f, HitLight),
                [SfxId.HitHeavy]   = Make("sfx_hitHeavy", 0.26f, HitHeavy),
                [SfxId.Block]      = Make("sfx_block", 0.14f, Block),
                [SfxId.Parry]      = Make("sfx_parry", 0.45f, Parry),
                [SfxId.Fireball]   = Make("sfx_fireball", 0.32f, Fireball),
                [SfxId.Super]      = Make("sfx_super", 0.60f, Super),
                [SfxId.Jump]       = Make("sfx_jump", 0.12f, Jump),
                [SfxId.Dash]       = Make("sfx_dash", 0.10f, Dash),
                [SfxId.KO]         = Make("sfx_ko", 0.60f, KO),
                [SfxId.RoundStart] = Make("sfx_roundStart", 0.50f, RoundStart),
                [SfxId.UiMove]     = Make("sfx_uiMove", 0.06f, UiMove),
                [SfxId.UiConfirm]  = Make("sfx_uiConfirm", 0.13f, UiConfirm),
            };
            return map;
        }

        /// <summary>A looping battle-drum bed (8 beats @ 120 BPM = 4.0s, loops seamlessly).</summary>
        public static AudioClip BuildMusicLoop()
        {
            return Make("music_battle", 4.0f, MusicLoop, loop: true);
        }

        // ---- clip definitions: f(t, sampleIndex) -> sample in [-1, 1] ----

        private static float LightSwing(float t, int i)
        {
            float swell = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.13f));
            return 0.4f * swell * Noise() + 0.1f * swell * Mathf.Sin(Tau * 420f * t);
        }

        private static float HeavySwing(float t, int i)
        {
            float swell = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.20f));
            float air = 0.45f * swell * Noise();
            float rumble = 0.2f * swell * Mathf.Sin(Tau * 130f * t);
            return air + rumble;
        }

        private static float Whiff(float t, int i)
        {
            return 0.22f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.14f)) * Noise();
        }

        private static float HitLight(float t, int i)
        {
            // Soft padded "thump": rounded low-mid body, only a whisper of softened transient.
            float env = Mathf.Exp(-t * 26f);
            float body = Mathf.Sin(Tau * 150f * t);
            float sub = Mathf.Sin(Tau * 90f * t);
            float transient = Noise() * Mathf.Exp(-t * 80f) * 0.15f; // gentle, fast-gone
            return env * (0.55f * body + 0.4f * sub) + transient;
        }

        private static float HitHeavy(float t, int i)
        {
            // Deeper, rounder thud — warmth over crack.
            float env = Mathf.Exp(-t * 9f);
            float bodyFreq = 62f + 38f * Mathf.Exp(-t * 12f);
            float body = Mathf.Sin(Tau * bodyFreq * t);
            float sub = Mathf.Sin(Tau * 48f * t);
            float transient = Noise() * Mathf.Exp(-t * 38f) * 0.16f;
            return env * (0.6f * body + 0.35f * sub) + transient;
        }

        private static float Block(float t, int i)
        {
            float env = Mathf.Exp(-t * 22f);
            float metal = Mathf.Sin(Tau * 900f * t) + 0.6f * Mathf.Sin(Tau * 1350f * t) + 0.4f * Mathf.Sin(Tau * 1820f * t);
            float chk = Noise() * Mathf.Exp(-t * 70f);
            return 0.22f * env * metal + 0.3f * chk;
        }

        private static float Parry(float t, int i)
        {
            float env = Mathf.Exp(-t * 6f);
            float ring = Mathf.Sin(Tau * 1600f * t) + 0.7f * Mathf.Sin(Tau * 2400f * t) + 0.5f * Mathf.Sin(Tau * 3200f * t);
            float spark = Noise() * Mathf.Exp(-t * 40f);
            return 0.16f * env * ring + 0.3f * spark;
        }

        private static float Fireball(float t, int i)
        {
            float dur = 0.32f;
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / dur));
            float freq = 600f - 380f * Mathf.Clamp01(t / dur);
            float tone = Mathf.Sin(Tau * freq * t);
            float crackle = Noise();
            return env * (0.3f * tone + 0.35f * crackle);
        }

        private static float Super(float t, int i)
        {
            float a = Mathf.Clamp01(t / 0.60f);
            float swell = Mathf.Sin(Mathf.PI * a);
            float chord = Mathf.Sin(Tau * 220f * t) + Mathf.Sin(Tau * 330f * t) + Mathf.Sin(Tau * 440f * t);
            float boom = Mathf.Sin(Tau * 80f * t) * Mathf.Exp(-Mathf.Max(0f, t - 0.30f) * 8f);
            float shimmer = Noise() * 0.2f * swell;
            return 0.16f * swell * chord + 0.4f * boom + shimmer;
        }

        private static float Jump(float t, int i)
        {
            float env = Mathf.Exp(-t * 12f);
            float freq = 250f + 500f * Mathf.Clamp01(t / 0.12f);
            return 0.3f * env * Mathf.Sin(Tau * freq * t);
        }

        private static float Dash(float t, int i)
        {
            return 0.3f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.10f)) * Noise();
        }

        private static float KO(float t, int i)
        {
            float env = Mathf.Exp(-t * 5f);
            float freq = 40f + 120f * Mathf.Exp(-t * 3f);
            float body = Mathf.Sin(Tau * freq * t);
            float n = Noise() * Mathf.Exp(-t * 30f);
            return env * 0.6f * body + 0.3f * n;
        }

        private static float RoundStart(float t, int i)
        {
            float env = Mathf.Exp(-t * 4f);
            float gong = Mathf.Sin(Tau * 330f * t) + 0.6f * Mathf.Sin(Tau * 660f * t) + 0.4f * Mathf.Sin(Tau * 495f * t);
            float n = Noise() * Mathf.Exp(-t * 50f);
            return 0.22f * env * gong + 0.2f * n;
        }

        private static float UiMove(float t, int i)
        {
            return 0.25f * Mathf.Exp(-t * 30f) * Mathf.Sin(Tau * 700f * t);
        }

        private static float UiConfirm(float t, int i)
        {
            float freq = t < 0.05f ? 500f : 850f;
            return 0.28f * Mathf.Exp(-t * 12f) * Mathf.Sin(Tau * freq * t);
        }

        private static float MusicLoop(float t, int i)
        {
            const float beat = 0.5f; // 120 BPM
            int beatIndex = (int)(t / beat) % 8;
            float bt = t - (int)(t / beat) * beat; // time since current beat

            // Sustained low drone — integer cycle counts over the 4s loop keep it seamless.
            float drone = 0.10f * (Mathf.Sin(Tau * 110f * t) + 0.6f * Mathf.Sin(Tau * 165f * t));

            // Kick on even beats. Uses bt so every hit is identical and loops cleanly.
            float kick = 0f;
            if (beatIndex % 2 == 0)
            {
                float kEnv = Mathf.Exp(-bt * 18f);
                float kFreq = 45f + 110f * Mathf.Exp(-bt * 9f);
                kick = Mathf.Sin(Tau * kFreq * bt) * kEnv * 0.5f;
            }

            // Tom + air on odd beats.
            float tom = 0f;
            if (beatIndex % 2 == 1)
            {
                float tEnv = Mathf.Exp(-bt * 24f);
                tom = Mathf.Sin(Tau * 180f * bt) * tEnv * 0.28f + Noise() * tEnv * 0.08f;
            }

            // Accent crack on the downbeat (beat 0) for a bar feel.
            float accent = 0f;
            if (beatIndex == 0)
                accent = Noise() * Mathf.Exp(-bt * 60f) * 0.15f;

            return drone + kick + tom + accent;
        }

        // ---- core synth helper ----

        private static AudioClip Make(string name, float seconds, System.Func<float, int, float> fn, bool loop = false)
        {
            int count = Mathf.Max(1, (int)(seconds * SampleRate));
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                data[i] = Mathf.Clamp(fn(t, i), -1f, 1f);
            }
            // Tiny fade at both ends of one-shots to avoid clicks (skip for seamless loops).
            if (!loop)
            {
                int fade = Mathf.Min(64, count / 8);
                for (int k = 0; k < fade; k++)
                {
                    float g = (float)k / fade;
                    data[k] *= g;
                    data[count - 1 - k] *= g;
                }
            }
            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
