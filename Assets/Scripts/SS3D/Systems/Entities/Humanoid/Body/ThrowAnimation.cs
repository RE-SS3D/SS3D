namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// Throw clips and their release timing. The editor rebuild imports and trims the clips from this
    /// table and wires one Upper Body state per entry; the server launches the item at
    /// <see cref="Clip.ReleaseSeconds"/> after the throw starts, so the item leaves when the hand does.
    /// </summary>
    public static class ThrowAnimation
    {
        /// <summary>Mixamo exports at 30 fps; frame numbers below are source FBX frames.</summary>
        public const float ClipFps = 30f;

        /// <summary>Index into <see cref="Variants"/> (sent as AttackVariant with the Throw trigger).</summary>
        public const int Overhand = 0;
        public const int Grenade = 1;

        /// <summary>Normalized time the throw states leave for the hold pose, and the blend length.</summary>
        public const float ExitTime = 0.85f;
        public const float ExitSeconds = 0.25f;

        /// <summary>Blend from whatever the upper body was doing into the wind-up.</summary>
        public const float EntrySeconds = 0.1f;

        public readonly struct Clip
        {
            /// <summary>Upper Body state name.</summary>
            public readonly string StateName;
            /// <summary>Path relative to Assets/Art/Animations.</summary>
            public readonly string File;
            public readonly string ClipName;
            public readonly int FirstFrame;
            public readonly int LastFrame;
            /// <summary>Source frame where the item leaves the hand (hand moving forward, in front of the head).</summary>
            public readonly int ReleaseFrame;

            public Clip(string stateName, string file, string clipName, int firstFrame, int lastFrame, int releaseFrame)
            {
                StateName = stateName;
                File = file;
                ClipName = clipName;
                FirstFrame = firstFrame;
                LastFrame = lastFrame;
                ReleaseFrame = releaseFrame;
            }

            public float LengthSeconds => (LastFrame - FirstFrame) / ClipFps;
            public float ReleaseSeconds => (ReleaseFrame - FirstFrame) / ClipFps;
        }

        public static readonly Clip[] Variants =
        {
            // Mixamo "Throw": overhand. Frames 0–7 are idle; the hand passes the head moving forward at 25.
            new("Throw", "Throw/X Bot@Throw.fbx", "Mix_Throw", 8, 56, 25),
            // Mixamo "Throw Grenade While Running": compact overhand lob. Only the Upper Body plays, so the
            // run in the legs never shows. Release (peak forward hand speed) at 51.
            new("Throw Grenade", "Throw/X Bot@Run And Throw Grenade.fbx", "Mix_RunAndThrowGrenade", 36, 64, 51),
        };

        public static bool TryGetClip(int variant, out Clip clip)
        {
            if (variant >= 0 && variant < Variants.Length)
            {
                clip = Variants[variant];
                return true;
            }

            clip = default;
            return false;
        }

        /// <summary>Seconds from the throw starting until the item leaves the hand.</summary>
        public static float ReleaseSeconds(int variant) =>
            TryGetClip(variant, out Clip clip) ? clip.ReleaseSeconds : Variants[Overhand].ReleaseSeconds;

        /// <summary>Seconds the throw keeps the Upper Body layer up (clip plus its exit blend).</summary>
        public static float PlaySeconds(int variant) =>
            (TryGetClip(variant, out Clip clip) ? clip.LengthSeconds : Variants[Overhand].LengthSeconds) * ExitTime
            + ExitSeconds;
    }
}
