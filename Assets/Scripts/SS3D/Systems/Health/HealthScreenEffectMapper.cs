using SS3D.Systems.ScreenEffects;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Maps a local player's <see cref="HealthSnapshot"/> onto health-driven
    /// <see cref="ScreenEffectType"/> intensities. Temperature/fire effects are left alone (atmos).
    /// </summary>
    public static class HealthScreenEffectMapper
    {
        /// <summary>Blood-loss tunnel vision starts fading in below this volume ratio.</summary>
        public const float BloodLossSoftStart = 0.85f;

        public struct Intensities
        {
            public float DyingCritical;
            public float BloodLossTunnelVision;
            public float LowOxygen;
            public float Concussion;
            public float Unconscious;
        }

        public static Intensities Compute(HealthSnapshot snapshot)
        {
            if (snapshot.State == HealthState.Dead)
            {
                return default;
            }

            return new Intensities
            {
                DyingCritical = ComputeDyingCritical(snapshot),
                BloodLossTunnelVision = ComputeBloodLoss(snapshot.Pools.BloodVolumeRatio),
                LowOxygen = ComputeLowOxygen(snapshot.Pools.OxyDebt),
                Concussion = ComputeConcussion(snapshot.BrainFunctionPercent),
                Unconscious = snapshot.IsConscious ? 0f : 1f,
            };
        }

        public static void Apply(HealthSnapshot snapshot, ScreenEffectsSubSystem effects)
        {
            if (effects == null)
            {
                return;
            }

            Intensities intensities = Compute(snapshot);
            effects.SetEffect(ScreenEffectType.DyingCritical, intensities.DyingCritical);
            effects.SetEffect(ScreenEffectType.BloodLossTunnelVision, intensities.BloodLossTunnelVision);
            effects.SetEffect(ScreenEffectType.LowOxygen, intensities.LowOxygen);
            effects.SetEffect(ScreenEffectType.Concussion, intensities.Concussion);
            effects.SetEffect(ScreenEffectType.Unconscious, intensities.Unconscious);
        }

        public static void Clear(ScreenEffectsSubSystem effects)
        {
            if (effects == null)
            {
                return;
            }

            effects.SetEffect(ScreenEffectType.DyingCritical, 0f);
            effects.SetEffect(ScreenEffectType.BloodLossTunnelVision, 0f);
            effects.SetEffect(ScreenEffectType.LowOxygen, 0f);
            effects.SetEffect(ScreenEffectType.Concussion, 0f);
            effects.SetEffect(ScreenEffectType.Unconscious, 0f);
        }

        private static float ComputeDyingCritical(HealthSnapshot snapshot)
        {
            if (snapshot.IsCardiacArrest)
            {
                return 1f;
            }

            if (snapshot.State != HealthState.Critical)
            {
                return 0f;
            }

            float brainFactor = Mathf.InverseLerp(
                100f,
                HealthConstants.ConsciousnessBrainFunctionPercent,
                snapshot.BrainFunctionPercent);
            return Mathf.Clamp01(0.5f + 0.5f * brainFactor);
        }

        private static float ComputeBloodLoss(float bloodVolumeRatio)
        {
            return Mathf.Clamp01(Mathf.InverseLerp(
                BloodLossSoftStart,
                HealthConstants.CriticalBloodVolumeRatio,
                bloodVolumeRatio));
        }

        private static float ComputeLowOxygen(float oxyDebt)
        {
            return Mathf.Clamp01(Mathf.InverseLerp(0f, HealthConstants.CriticalOxyDebt, oxyDebt));
        }

        private static float ComputeConcussion(float brainFunctionPercent)
        {
            return Mathf.Clamp01(Mathf.InverseLerp(
                100f,
                HealthConstants.ConsciousnessBrainFunctionPercent,
                brainFunctionPercent));
        }
    }
}
