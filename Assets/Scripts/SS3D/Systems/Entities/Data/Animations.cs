using UnityEngine;

namespace SS3D.Systems.Entities.Data
{
    public static class Animations
    {
        public static class Humanoid
        {
            public static readonly int MovementSpeed = Animator.StringToHash("Speed");

            // Base/Movement layer.
            public static readonly int Sit = Animator.StringToHash("Sit");
            public static readonly int Prone = Animator.StringToHash("Prone");
            public static readonly int LegInjury = Animator.StringToHash("LegInjury");

            // Injury layer (additive), one trigger per flinchable body region.
            public static readonly int HurtArmLeft = Animator.StringToHash("HurtArmLeft");
            public static readonly int HurtArmRight = Animator.StringToHash("HurtArmRight");
            public static readonly int HurtLegLeft = Animator.StringToHash("HurtLegLeft");
            public static readonly int HurtLegRight = Animator.StringToHash("HurtLegRight");
            public static readonly int HurtHeadFront = Animator.StringToHash("HurtHeadFront");
            public static readonly int HurtHeadBack = Animator.StringToHash("HurtHeadBack");
            public static readonly int HurtTorsoFront = Animator.StringToHash("HurtTorsoFront");
            public static readonly int HurtTorsoBack = Animator.StringToHash("HurtTorsoBack");

            // Hold Left/Right layers - one int selector per arm (see HoldPose enum).
            public static readonly int HoldPoseLeft = Animator.StringToHash("HoldPoseLeft");
            public static readonly int HoldPoseRight = Animator.StringToHash("HoldPoseRight");

            // Emote layer.
            public static readonly int EmoteIndex = Animator.StringToHash("EmoteIndex");
            public static readonly int EmoteTrigger = Animator.StringToHash("EmoteTrigger");

            // Action layer (masked to upper body so it also plays while sitting/prone).
            public static readonly int HitTriggerLeft = Animator.StringToHash("HitTriggerLeft");
            public static readonly int HitTriggerRight = Animator.StringToHash("HitTriggerRight");
            public static readonly int ThrowTriggerLeft = Animator.StringToHash("ThrowTriggerLeft");
            public static readonly int ThrowTriggerRight = Animator.StringToHash("ThrowTriggerRight");
        }

        public static class Silicon
        {
            public static readonly int Power = Animator.StringToHash("Power");
        }
    }
}