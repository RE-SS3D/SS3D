using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// Melee AttackSwing clips and their strike timing. The editor rebuild imports and trims the clips
    /// from this table; <see cref="AnimationOrchestrator"/> uses it to play each swing's wind-up so the
    /// strike frame lands when the server applies damage (the weapon's effective windup), then plays
    /// the follow-through at natural speed.
    /// </summary>
    public static class MeleeSwingAnimation
    {
        /// <summary>Mixamo exports at 30 fps; frame numbers below are source FBX frames.</summary>
        public const float ClipFps = 30f;

        /// <summary>Wind-up playback bounds — beyond these the swing reads as slow-motion or a twitch.</summary>
        public const float MinWindupRate = 0.6f;
        public const float MaxWindupRate = 2.5f;

        /// <summary>Snapshot carries the windup in 20 ms steps over 5 bits (0 = unknown → natural speed).</summary>
        public const float WindupQuantumSeconds = 0.02f;
        public const int WindupQuantizedMax = 31;

        public readonly struct Swing
        {
            /// <summary>Upper Body state name (HumanoidIkController matches these).</summary>
            public readonly string StateName;
            /// <summary>Path relative to Assets/Art/Animations.</summary>
            public readonly string File;
            public readonly string ClipName;
            public readonly int FirstFrame;
            public readonly int LastFrame;
            /// <summary>Source frame where the weapon connects (peak hand speed through the target).</summary>
            public readonly int StrikeFrame;

            public Swing(string stateName, string file, string clipName, int firstFrame, int lastFrame, int strikeFrame)
            {
                StateName = stateName;
                File = file;
                ClipName = clipName;
                FirstFrame = firstFrame;
                LastFrame = lastFrame;
                StrikeFrame = strikeFrame;
            }

            public float LengthSeconds => (LastFrame - FirstFrame) / ClipFps;
            public float StrikeSeconds => (StrikeFrame - FirstFrame) / ClipFps;
            public float StrikeNormalizedTime => StrikeSeconds / LengthSeconds;
        }

        /// <summary>
        /// Indexed by AttackVariant (2 bits on the snapshot). Compact one-handed attacks from Pro Sword
        /// and Shield Pack: they keep the feet planted and return to the ready pose, unlike the axe
        /// pack's 2.3–3.2 s swings that struck at ~1 s and corkscrewed the torso over the legs.
        /// </summary>
        public static readonly Swing[] Variants =
        {
            // Mixamo "slash (5)": forehand sweep right → left. First frames are idle, trimmed.
            new("Attack Swing", "Pro Sword and Shield Pack/slash horizontal.fbx",
                "Mix_SwordShieldSlashHorizontal", 3, 41, 17),
            // Mixamo "attack (4)": short overhead chop.
            new("Attack Swing Downward", "Pro Sword and Shield Pack/chop overhead.fbx",
                "Mix_SwordShieldChopOverhead", 0, 30, 13),
            // Mixamo "slash (3)": cross-body wind-up then backhand sweep left → right.
            new("Attack Swing Backhand", "Pro Sword and Shield Pack/slash backhand.fbx",
                "Mix_SwordShieldSlashBackhand", 9, 50, 24),
        };

        public static bool TryGetSwing(int variant, out Swing swing)
        {
            if (variant >= 0 && variant < Variants.Length)
            {
                swing = Variants[variant];
                return true;
            }

            swing = default;
            return false;
        }

        /// <summary>Playback rate that puts the strike frame at <paramref name="windupSeconds"/> after the trigger.</summary>
        public static float ComputeWindupRate(int variant, float windupSeconds)
        {
            if (windupSeconds <= 0f || !TryGetSwing(variant, out Swing swing))
            {
                return 1f;
            }

            return Mathf.Clamp(swing.StrikeSeconds / windupSeconds, MinWindupRate, MaxWindupRate);
        }

        public static byte QuantizeWindup(float windupSeconds)
        {
            if (windupSeconds <= 0f)
            {
                return 0;
            }

            int steps = Mathf.RoundToInt(windupSeconds / WindupQuantumSeconds);
            return (byte)Mathf.Clamp(steps, 1, WindupQuantizedMax);
        }

        public static float DequantizeWindup(byte quantized)
        {
            return (quantized & WindupQuantizedMax) * WindupQuantumSeconds;
        }
    }
}
