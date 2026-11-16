using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Stub celestial sampler for solar generation. No world sun / station-rotation sim yet —
    /// azimuth sweeps and intensity rises/falls (with eclipse windows) on a fixed period.
    /// </summary>
    public static class SolarCycle
    {
        public const float DefaultPeriodSeconds = 600f;
        public const float DefaultEclipseFraction = 0.2f;

        /// <summary>Full day/night + eclipse cycle length in seconds.</summary>
        public static float PeriodSeconds { get; set; } = DefaultPeriodSeconds;

        /// <summary>Fraction of the period forced to zero intensity (centered on peak day).</summary>
        public static float EclipseFraction { get; set; } = DefaultEclipseFraction;

        /// <summary>When set, replaces <see cref="Time.time"/> for deterministic tests.</summary>
        public static float? TimeOverride { get; set; }

        public static void ResetDefaults()
        {
            PeriodSeconds = DefaultPeriodSeconds;
            EclipseFraction = DefaultEclipseFraction;
            TimeOverride = null;
        }

        public static float GetSunAzimuthDegrees(float? time = null)
        {
            float phase = GetPhase(time);
            return phase * 360f;
        }

        /// <summary>
        /// 0–1 illumination. Day half of the cycle follows a half-sine; night and eclipse windows are 0.
        /// </summary>
        public static float GetSunIntensity(float? time = null)
        {
            float phase = GetPhase(time);

            float eclipseHalf = Mathf.Clamp01(EclipseFraction) * 0.5f;
            const float eclipseCenter = 0.25f;
            if (Mathf.Abs(phase - eclipseCenter) < eclipseHalf)
            {
                return 0f;
            }

            // Sin(2π·phase): positive on [0, 0.5] (day), negative on [0.5, 1] (night → 0).
            return Mathf.Max(0f, Mathf.Sin(phase * Mathf.PI * 2f));
        }

        public static Direction GetNearestAimDirection(float azimuthDegrees)
        {
            float normalized = Mathf.Repeat(azimuthDegrees, 360f);
            int index = Mathf.RoundToInt(normalized / 45f) % 8;
            if (index < 0)
            {
                index += 8;
            }

            return (Direction)index;
        }

        /// <summary>
        /// 1 when the panel faces the sun, 0 when facing away (cosine of yaw delta, clamped).
        /// </summary>
        public static float ComputeAimFactor(Direction panelAim, float sunAzimuthDegrees)
        {
            float panelYaw = TileHelper.GetRotationAngle(panelAim);
            float delta = Mathf.DeltaAngle(panelYaw, sunAzimuthDegrees);
            return Mathf.Max(0f, Mathf.Cos(delta * Mathf.Deg2Rad));
        }

        private static float GetPhase(float? time)
        {
            float period = Mathf.Max(0.001f, PeriodSeconds);
            float t = time ?? TimeOverride ?? Time.time;
            return Mathf.Repeat(t / period, 1f);
        }
    }
}
