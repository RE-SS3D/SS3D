using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// Names and clips for the one-handed gun stance (Mixamo Pistol Handgun Locomotion Pack). The editor
    /// rebuild wires the animator from this table; <see cref="AnimationOrchestrator"/> drives it at runtime.
    /// </summary>
    /// <remarks>
    /// The pack's clips grip the gun with both hands (the hands stay ~4 cm apart). When the off-hand holds
    /// an item, that arm's Off Hand layer plays the relaxed idle arm over the pistol pose, so only the gun arm
    /// keeps the clip's grip.
    /// </remarks>
    public static class PistolStanceAnimation
    {
        /// <summary>Folder under Assets/Art/Animations.</summary>
        public const string Folder = "Pistol";

        /// <summary>Float, 0 = rifle hold, 1 = pistol hold. Blends Ranged Locomotion's leg trees.</summary>
        public const string HoldParameter = "PistolHold";

        /// <summary>
        /// Float, the strafe axis of both Ranged Locomotion trees: VelX, negated while MirrorUpperBody is on.
        /// A left-hand pistol mirrors Ranged Locomotion as well as the upper body, because a mirrored torso
        /// over unmirrored legs flips the torso's twist against the bladed hips and points the gun ~45° off to
        /// the side. A mirrored strafe-right clip steps left, so the trees read the negated axis.
        /// </summary>
        public const string StrafeParameter = "RangedVelX";

        /// <summary>Upper Body rest pose in the Ranged stance with a pistol (the pack's idle).</summary>
        public const string AimIdleState = "Pistol Aim Idle";

        /// <summary>The single state on each Off Hand layer; plays the peaceful idle's arm.</summary>
        public const string OffHandState = "Off Hand Hold";

        /// <summary>Layer whose mask covers only the left arm and fingers.</summary>
        public const string OffHandLeftLayer = "Off Hand Left";

        /// <summary>Layer whose mask covers only the right arm and fingers.</summary>
        public const string OffHandRightLayer = "Off Hand Right";

        /// <summary>
        /// Seconds for <see cref="HoldParameter"/> and the Off Hand layer weights to cross 0 → 1, so
        /// swapping a pistol for a rifle (or filling the off-hand) blends instead of popping.
        /// </summary>
        public const float BlendSeconds = 0.15f;

        public readonly struct Clip
        {
            /// <summary>File name inside <see cref="Folder"/>.</summary>
            public readonly string File;
            public readonly string ClipName;
            /// <summary>VelX / VelZ blend position (walk ring 0.3, run ring 1.0).</summary>
            public readonly Vector2 Position;

            public Clip(string file, string clipName, Vector2 position)
            {
                File = file;
                ClipName = clipName;
                Position = position;
            }
        }

        /// <summary>Peak forearm pitch of a shot's recoil kick (degrees, muzzle up).</summary>
        public const float ShotKickDegrees = 9f;

        /// <summary>Seconds from the shot to the kick's peak.</summary>
        public const float ShotKickRiseSeconds = 0.03f;

        /// <summary>Exponential decay time constant after the peak.</summary>
        public const float ShotKickDecaySeconds = 0.07f;

        /// <summary>After this the kick is under 0.1° and stops being applied.</summary>
        public const float ShotKickTotalSeconds = ShotKickRiseSeconds + 6f * ShotKickDecaySeconds;

        /// <summary>
        /// Kick angle <paramref name="elapsed"/> seconds after a shot: a fast linear rise, then an
        /// exponential settle. Zero before the shot and after <see cref="ShotKickTotalSeconds"/>.
        /// </summary>
        public static float EvaluateShotKick(float elapsed)
        {
            if (elapsed < 0f || elapsed >= ShotKickTotalSeconds)
            {
                return 0f;
            }

            if (elapsed < ShotKickRiseSeconds)
            {
                return ShotKickDegrees * elapsed / ShotKickRiseSeconds;
            }

            return ShotKickDegrees * Mathf.Exp(-(elapsed - ShotKickRiseSeconds) / ShotKickDecaySeconds);
        }

        public static readonly Clip Idle = new("pistol idle.fbx", "Mix_PistolIdle", Vector2.zero);

        /// <summary>
        /// Leg tree for the Ranged stance with a pistol. The pack has no diagonals and one strafe speed,
        /// so each strafe sits on both rings; the rebuild calibrates timeScale per child from root speed.
        /// "strafe (2)" moves right (+X), "strafe" moves left.
        /// </summary>
        public static readonly Clip[] Locomotion =
        {
            Idle,
            new("pistol walk.fbx", "Mix_PistolWalk", new Vector2(0f, 0.3f)),
            new("pistol strafe (2).fbx", "Mix_PistolStrafe2", new Vector2(0.3f, 0f)),
            new("pistol walk backward.fbx", "Mix_PistolWalkBackward", new Vector2(0f, -0.3f)),
            new("pistol strafe.fbx", "Mix_PistolStrafe", new Vector2(-0.3f, 0f)),
            new("pistol run.fbx", "Mix_PistolRun", new Vector2(0f, 1f)),
            new("pistol strafe (2).fbx", "Mix_PistolStrafe2", new Vector2(1f, 0f)),
            new("pistol run backward.fbx", "Mix_PistolRunBackward", new Vector2(0f, -1f)),
            new("pistol strafe.fbx", "Mix_PistolStrafe", new Vector2(-1f, 0f)),
        };
    }
}
