using SS3D.Systems.ScreenEffects;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Maps a local player's <see cref="HealthSnapshot"/> onto health-driven
    /// <see cref="ScreenEffectType"/> intensities. Temperature/fire effects are left alone (atmos).
    /// While dying/critical is active, low-oxygen screen intensity is attenuated so the red
    /// heartbeat cue is not washed out by the blue oxy vignette (alert stack still uses oxy debt).
    /// </summary>
    public static class HealthScreenEffectMapper
    {
        /// <summary>Blood-loss tunnel vision starts fading in below this volume ratio.</summary>
        public const float BloodLossSoftStart = 0.85f;

        /// <summary>Minimum DyingCritical intensity while in <see cref="HealthState.Critical"/>.</summary>
        public const float CriticalDyingFloor = 0.75f;

        /// <summary>
        /// At full DyingCritical, residual LowOxygen screen intensity multiplier (0 = mute oxy vignette).
        /// </summary>
        public const float LowOxygenUnderDyingFloor = 0.15f;

        public struct Intensities
        {
            public float DyingCritical;
            public float BloodLossTunnelVision;
            public float LowOxygen;
            public float Concussion;
            public float Unconscious;
            public float Shock;
            public float ShockBuildUp;
        }

        public static Intensities Compute(HealthSnapshot snapshot)
        {
            if (snapshot.State == HealthState.Dead)
            {
                return default;
            }

            float dying = ComputeDyingCritical(snapshot);
            float lowOxygen = ComputeLowOxygen(snapshot.Pools.OxyDebt);
            if (dying > 0f)
            {
                lowOxygen *= Mathf.Lerp(1f, LowOxygenUnderDyingFloor, dying);
            }

            return new Intensities
            {
                DyingCritical = dying,
                BloodLossTunnelVision = ComputeBloodLoss(snapshot.Pools.BloodVolumeRatio),
                LowOxygen = lowOxygen,
                Concussion = Mathf.Max(ComputeConcussion(snapshot.BrainFunctionPercent), snapshot.HeadTraumaBlur),
                Unconscious = snapshot.IsConscious ? 0f : 1f,
                Shock = snapshot.IsConscious ? ShockSimulation.FeelIntensity(snapshot.Shock) : 0f,
                ShockBuildUp = ShockSimulation.IsGoingDown(snapshot) ? 1f : 0f,
            };
        }

        public static void Apply(HealthSnapshot snapshot, ScreenEffectsSubSystem effects)
        {
            if (effects == null || effects.IsDebugOverrideActive)
            {
                return;
            }

            Intensities intensities = Compute(snapshot);
            effects.SetEffect(ScreenEffectType.DyingCritical, intensities.DyingCritical);
            effects.SetEffect(ScreenEffectType.BloodLossTunnelVision, intensities.BloodLossTunnelVision);
            effects.SetEffect(ScreenEffectType.LowOxygen, intensities.LowOxygen);
            effects.SetEffect(ScreenEffectType.Concussion, intensities.Concussion);
            effects.SetEffect(ScreenEffectType.Unconscious, intensities.Unconscious);
            effects.SetEffect(ScreenEffectType.Shock, intensities.Shock);
            effects.SetEffect(ScreenEffectType.ShockBuildUp, intensities.ShockBuildUp);
        }

        public static void Clear(ScreenEffectsSubSystem effects)
        {
            if (effects == null || effects.IsDebugOverrideActive)
            {
                return;
            }

            effects.SetEffect(ScreenEffectType.DyingCritical, 0f);
            effects.SetEffect(ScreenEffectType.BloodLossTunnelVision, 0f);
            effects.SetEffect(ScreenEffectType.LowOxygen, 0f);
            effects.SetEffect(ScreenEffectType.Concussion, 0f);
            effects.SetEffect(ScreenEffectType.Unconscious, 0f);
            effects.SetEffect(ScreenEffectType.Shock, 0f);
            effects.SetEffect(ScreenEffectType.ShockBuildUp, 0f);
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
            return Mathf.Clamp01(CriticalDyingFloor + (1f - CriticalDyingFloor) * brainFactor);
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
