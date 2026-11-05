using SS3D.Systems.Audio;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Maps a local player's <see cref="HealthSnapshot"/> onto personal heartbeat and labored
    /// breathing cues (audio.md §4, worked example C). Deliberately diverges from
    /// <see cref="HealthScreenEffectMapper"/>'s cardiac-arrest handling: the screen vignette stays at
    /// full intensity through cardiac arrest (still dying, still worth showing), but the heartbeat
    /// sound represents an actual beating heart, so it goes silent the moment the heart stops and
    /// resumes only when a defib restarts it — "resuming on a successful defib" only makes sense if
    /// something was silenced first.
    /// Breathing is max-merged with stamina in <see cref="PersonalAudioSubSystem"/>.
    /// </summary>
    public static class HealthPersonalAudioMapper
    {
        /// <summary>Oxy debt at/above this starts labored breathing (matches soft low-O₂ alert).</summary>
        public const float BreathingOxyDebtStart = HealthConstants.LowOxygenAlertSoftStart;

        /// <summary>Oxy debt at full breathing intensity.</summary>
        public const float BreathingOxyDebtFull = HealthConstants.CriticalOxyDebt;

        private const float InternalsBreathingIntensity = 0.4f;

        public static float ComputeHeartbeatIntensity(HealthSnapshot snapshot)
        {
            // IsCardiacArrest is the operative flag (mirrors HealthScreenEffectMapper.ComputeDyingCritical,
            // which checks it before State) — the heart has stopped, so there's nothing to beat until a
            // defib restarts it, regardless of what State currently reads.
            if (snapshot.IsCardiacArrest)
            {
                return 0f;
            }

            if (snapshot.State != HealthState.Critical)
            {
                // Dead: brain function reaching zero silences it entirely.
                // Healthy: no cue outside critical/dying (audio.md §4).
                return 0f;
            }

            float brainFactor = Mathf.InverseLerp(
                100f,
                HealthConstants.ConsciousnessBrainFunctionPercent,
                snapshot.BrainFunctionPercent);
            return Mathf.Clamp01(0.4f + 0.6f * brainFactor);
        }

        /// <summary>
        /// Labored breathing from oxy debt and critical state. Vacuum / low breathability amplify
        /// via elevated oxy debt already — no separate env path required.
        /// </summary>
        public static float ComputeBreathingIntensity(HealthSnapshot snapshot)
        {
            if (snapshot.State == HealthState.Dead || !snapshot.IsConscious)
            {
                return 0f;
            }

            float fromOxy = Mathf.Clamp01(Mathf.InverseLerp(
                BreathingOxyDebtStart,
                BreathingOxyDebtFull,
                snapshot.Pools.OxyDebt));

            float fromCritical = 0f;
            if (snapshot.State == HealthState.Critical && !snapshot.IsCardiacArrest)
            {
                fromCritical = 0.55f;
            }

            return Mathf.Max(fromOxy, fromCritical);
        }

        /// <summary>Internals are audible while the tank still gives a breath (armor.md §6).</summary>
        public static float ComputeInternalsIntensity(HealthSnapshot snapshot)
        {
            bool breathing = snapshot.Environment.OnInternals
                && snapshot.Environment.TankFill > 0f
                && snapshot.IsConscious
                && snapshot.State != HealthState.Dead;
            return breathing ? InternalsBreathingIntensity : 0f;
        }

        public static void Apply(HealthSnapshot snapshot, PersonalAudioSubSystem personalAudio)
        {
            if (personalAudio == null)
            {
                return;
            }

            personalAudio.SetHeartbeatIntensity(ComputeHeartbeatIntensity(snapshot));
            personalAudio.SetHealthBreathingIntensity(ComputeBreathingIntensity(snapshot));
            personalAudio.SetInternalsBreathingIntensity(ComputeInternalsIntensity(snapshot));
        }

        public static void Clear(PersonalAudioSubSystem personalAudio)
        {
            if (personalAudio == null)
            {
                return;
            }

            personalAudio.SetHeartbeatIntensity(0f);
            personalAudio.ClearHealthBreathing();
        }
    }
}
