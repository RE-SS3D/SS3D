using System;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// How a body reacts to a hit (combat.md §5), from the damage before armor so a hardsuit still flinches: every hit
    /// flinches and kicks the victim's view, a heavy one staggers and cancels what they were doing, and a hit on an
    /// already wounded leg trips a sprinting body (health.md §5).
    /// </summary>
    public static class HitReaction
    {
        /// <summary>Gait speed (walk 0.3, run 1.0) above which the body counts as sprinting.</summary>
        public const float SprintGaitThreshold = 0.65f;

        public static bool IsStagger(float preArmorDamage) => preArmorDamage >= HealthConstants.HeavyHitStaggerDamage;

        /// <summary>Flinch strength 0–3 for the additive jolt: a pistol round is a twitch, a heavy hit a jolt.</summary>
        public static byte FlinchStrength(float preArmorDamage)
        {
            float t = Math.Clamp(preArmorDamage / HealthConstants.HeavyHitStaggerDamage, 0f, 1f);
            return (byte)Math.Round(t * 3f);
        }

        /// <summary>Victim view kick, in camera-shake amplitude.</summary>
        public static float ViewKickAmplitude(float preArmorDamage)
        {
            float t = Math.Clamp(preArmorDamage / HealthConstants.HeavyHitStaggerDamage, 0f, 1f);
            return HealthConstants.ViewKickMinAmplitude
                + ((HealthConstants.ViewKickMaxAmplitude - HealthConstants.ViewKickMinAmplitude) * t);
        }

        public static bool IsSprinting(float gaitSpeed) => gaitSpeed >= SprintGaitThreshold;

        /// <summary>A hit on a leg already at Wound or worse trips a sprinting body.</summary>
        public static bool ShouldTrip(BodyZone zone, WoundSeverity legBeforeHit, float gaitSpeed)
        {
            return zone is BodyZone.LeftLeg or BodyZone.RightLeg
                && legBeforeHit >= WoundSeverity.Wound
                && IsSprinting(gaitSpeed);
        }
    }
}
