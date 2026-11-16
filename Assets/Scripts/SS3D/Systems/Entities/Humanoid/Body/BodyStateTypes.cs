using System;

namespace SS3D.Systems.Entities.Humanoid.Body
{
    public enum BodyState : byte
    {
        Locomotion = 0,
        Seated = 1,
        Crawling = 2,
        Staggered = 3,
        Ragdoll = 4,
        Dead = 5,
        Unconscious = 6,
    }

    public enum LocomotionMode : byte
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
        LimpLeft = 3,
        LimpRight = 4,
        Floating = 5,
    }

    public enum LimpSide : byte
    {
        None = 0,
        Left = 1,
        Right = 2,
    }

    public enum HumanoidCombatMode : byte
    {
        Peaceful = 0,
        Melee = 1,
        Ranged = 2,
    }

    public static class HumanoidCombatModeExtensions
    {
        public static bool IsCombat(this HumanoidCombatMode mode) =>
            mode == HumanoidCombatMode.Melee || mode == HumanoidCombatMode.Ranged;
    }

    public enum ArmHoldPose : byte
    {
        Default = 0,
        Item = 1,
        Weapon = 2,
    }

    public enum AnimationTriggerId : byte
    {
        None = 0,
        AttackSwing = 1,
        AttackStab = 2,
        Throw = 3,
        Emote = 4,
        Flinch = 5,
        FireRifle = 6,
        Reload = 7,
    }

    [Serializable]
    public struct BodyCapabilities
    {
        public bool CanMove;
        public bool CanRotate;
        public bool CanRun;
        public bool CanUseHands;
        public bool CanInteract;
        public bool CanBeInterrupted;
        public bool CanClimb;

        public static BodyCapabilities ForState(BodyState state)
        {
            return state switch
            {
                BodyState.Locomotion => FullLocomotion(),
                BodyState.Seated => new BodyCapabilities
                {
                    CanMove = false,
                    CanRotate = false,
                    CanRun = false,
                    CanUseHands = true,
                    CanInteract = true,
                    CanBeInterrupted = true,
                },
                BodyState.Crawling => new BodyCapabilities
                {
                    CanMove = true,
                    CanRotate = true,
                    CanRun = false,
                    CanUseHands = true,
                    CanInteract = true,
                    CanBeInterrupted = true,
                },
                BodyState.Staggered => new BodyCapabilities
                {
                    CanMove = true,
                    CanRotate = true,
                    CanRun = false,
                    CanUseHands = false,
                    CanInteract = false,
                    CanBeInterrupted = true,
                },
                BodyState.Ragdoll => None(),
                BodyState.Dead => None(),
                BodyState.Unconscious => None(),
                _ => FullLocomotion(),
            };
        }

        public static BodyCapabilities FullLocomotion() => new()
        {
            CanMove = true,
            CanRotate = true,
            CanRun = true,
            CanUseHands = true,
            CanInteract = true,
            CanBeInterrupted = true,
        };

        public static BodyCapabilities None() => default;
    }
}
