using UnityEngine;

namespace SS3D.Systems.Stamina
{
    /// <summary>
    /// Pure sprint rules shared by the stamina controller and movement (Documents/design/stamina.md §2):
    /// only movement above walking speed drains the pool.
    /// </summary>
    public static class SprintExertion
    {
        /// <summary>
        /// Stamina per second at full run speed. With regen paused while sprinting, a full
        /// <see cref="StaminaFactory.DefaultMax"/> pool lasts about 10 s of nonstop sprint.
        /// </summary>
        public const float SprintDrainPerSecond = 1f;

        /// <summary>Regen resumes this long after the last sprint drain (refill happens at rest, not mid-sprint).</summary>
        public const float RegenDelayAfterSprint = 1f;

        /// <summary>Stamina fraction below which movement starts slowing; above it you move at full speed.</summary>
        public const float SlowdownThreshold = 0.25f;

        /// <summary>Movement speed multiplier at empty stamina.</summary>
        public const float ExhaustedSpeedFactor = 0.55f;

        /// <summary>
        /// 0 at or below walk gait, 1 at run gait. Gait units match HumanoidController
        /// (walk 0.3, run 1.0), so walking and standing never drain.
        /// </summary>
        public static float DrainFraction(float gaitSpeed, float walkGait, float runGait)
        {
            if (runGait <= walkGait)
            {
                return 0f;
            }

            return Mathf.Clamp01((gaitSpeed - walkGait) / (runGait - walkGait));
        }

        /// <summary>
        /// Movement multiplier from <c>ExertionPenalty</c> (0 rested .. 1 empty): full speed down to
        /// <see cref="SlowdownThreshold"/> stamina, then a ramp to <see cref="ExhaustedSpeedFactor"/>.
        /// Reticle bloom and melee timing keep the linear penalty; only movement uses this curve.
        /// </summary>
        public static float MovementSpeedFactor(float exertionPenalty)
        {
            float stamina = 1f - Mathf.Clamp01(exertionPenalty);
            if (stamina >= SlowdownThreshold)
            {
                return 1f;
            }

            return Mathf.Lerp(ExhaustedSpeedFactor, 1f, stamina / SlowdownThreshold);
        }

        public static bool ShouldRegenerate(float secondsSinceSprintDrain)
        {
            return secondsSinceSprintDrain >= RegenDelayAfterSprint;
        }
    }
}
