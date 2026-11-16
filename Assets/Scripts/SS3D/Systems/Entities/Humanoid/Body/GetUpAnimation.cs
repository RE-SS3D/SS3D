using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// Stand-up clips played after a knockdown ends. The editor rebuild imports and trims the clips from
    /// this table and sets each Base Layer state's speed; <see cref="Ragdoll"/> blends the ragdoll into the
    /// clip's first frame, plays the state, and hands control back at <see cref="BlendOutStart"/>.
    /// </summary>
    public static class GetUpAnimation
    {
        /// <summary>Mixamo exports at 30 fps; frame numbers below are source FBX frames.</summary>
        public const float ClipFps = 30f;

        /// <summary>
        /// Time a stand-up should take (combat.md §5 puts the punishable window on the ground, not here).
        /// Clips longer than this play faster, up to <see cref="MaxPlaybackRate"/>.
        /// </summary>
        public const float TargetSeconds = 1.2f;

        /// <summary>Beyond this the stand-up reads as a twitch (the melee swing limit is the same).</summary>
        public const float MaxPlaybackRate = 2.5f;

        /// <summary>
        /// Normalized time where movement comes back and the state blends into locomotion over the rest
        /// of the clip, so a held direction walks out of the last ~20 % instead of waiting for the end.
        /// </summary>
        public const float BlendOutStart = 0.8f;

        /// <summary>Seconds the ragdoll blends into the clip's first frame before the clip plays.</summary>
        public const float BlendInSeconds = 0.5f;

        public readonly struct Clip
        {
            /// <summary>Base Layer state name.</summary>
            public readonly string StateName;
            /// <summary>Path relative to Assets/Art/Animations.</summary>
            public readonly string File;
            public readonly string ClipName;
            public readonly int FirstFrame;
            public readonly int LastFrame;

            public Clip(string stateName, string file, string clipName, int firstFrame, int lastFrame)
            {
                StateName = stateName;
                File = file;
                ClipName = clipName;
                FirstFrame = firstFrame;
                LastFrame = lastFrame;
            }

            public float LengthSeconds => (LastFrame - FirstFrame) / ClipFps;
            public float PlaybackRate => ComputePlaybackRate(LengthSeconds);
            public float DurationSeconds => LengthSeconds / PlaybackRate;
            /// <summary>Wall-clock seconds from <see cref="BlendOutStart"/> to the clip's end.</summary>
            public float BlendOutSeconds => (1f - BlendOutStart) * DurationSeconds;
        }

        // Mixamo "Stand Up" (from the back). Frames 0-40 lie still; by 186 the body stands and idles.
        public static readonly Clip FaceUp = new(
            "GettingUpFaceUp", "GetUp/X Bot@Stand Up From Back.fbx", "Mix_StandUpFromBack", 40, 186);

        // Mixamo "Standing Up" (from the stomach). Frames 0-96 lie and shift; after 258 it only re-plants the feet.
        public static readonly Clip FaceDown = new(
            "GettingUpFaceDown", "GetUp/X Bot@Standing Up From Stomach.fbx", "Mix_StandUpFromStomach", 96, 258);

        public static readonly Clip[] All = { FaceUp, FaceDown };

        public static Clip For(bool facingDown) => facingDown ? FaceDown : FaceUp;

        public static float ComputePlaybackRate(float lengthSeconds)
        {
            if (lengthSeconds <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp(lengthSeconds / TargetSeconds, 1f, MaxPlaybackRate);
        }
    }
}
