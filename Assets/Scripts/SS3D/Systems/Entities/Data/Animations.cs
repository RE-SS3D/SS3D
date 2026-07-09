using UnityEngine;

namespace SS3D.Systems.Entities.Data
{
    public static class Animations
    {
        public static class Humanoid
        {
            public static readonly int MovementSpeed = Animator.StringToHash("Speed");
            public static readonly int Floating = Animator.StringToHash("Floating");
            public static readonly int LimpSide = Animator.StringToHash("LimpSide");
            public static readonly int IsCrawling = Animator.StringToHash("IsCrawling");
            public static readonly int IsDragging = Animator.StringToHash("IsDragging");
            public static readonly int ArmHold = Animator.StringToHash("ArmHold");
            public static readonly int InjuredArmLeft = Animator.StringToHash("InjuredArmLeft");
            public static readonly int InjuredArmRight = Animator.StringToHash("InjuredArmRight");
            public static readonly int IsSeated = Animator.StringToHash("IsSeated");
            public static readonly int CombatMode = Animator.StringToHash("CombatMode");
            public static readonly int AimYaw = Animator.StringToHash("AimYaw");
            public static readonly int AttackSwing = Animator.StringToHash("AttackSwing");
            public static readonly int AttackStab = Animator.StringToHash("AttackStab");
            public static readonly int Throw = Animator.StringToHash("Throw");
            public static readonly int Emote = Animator.StringToHash("Emote");
            public static readonly int Flinch = Animator.StringToHash("Flinch");

            public static int GetTriggerHash(Humanoid.Body.AnimationTriggerId trigger)
            {
                return trigger switch
                {
                    Humanoid.Body.AnimationTriggerId.AttackSwing => AttackSwing,
                    Humanoid.Body.AnimationTriggerId.AttackStab => AttackStab,
                    Humanoid.Body.AnimationTriggerId.Throw => Throw,
                    Humanoid.Body.AnimationTriggerId.Emote => Emote,
                    Humanoid.Body.AnimationTriggerId.Flinch => Flinch,
                    _ => 0,
                };
            }
        }

        public static class Silicon
        {
            public static readonly int Power = Animator.StringToHash("Power");
            public static readonly int MovementSpeed = Animator.StringToHash("Speed");
        }
    }
}
