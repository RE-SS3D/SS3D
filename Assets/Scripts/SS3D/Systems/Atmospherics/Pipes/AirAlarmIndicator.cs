namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>What the alarm's face shows: steady green, blinking blue or red, or a fast red flash.</summary>
    public enum AirAlarmIndicator : byte
    {
        Off = 0,
        Nominal = 1,
        Repressurizing = 2,
        Depressurizing = 3,
        Danger = 4,
    }

    /// <summary>Which way the room's pressure is moving, smoothed over atmos ticks.</summary>
    public enum AirAlarmPressureTrend : byte
    {
        Steady = 0,
        Rising = 1,
        Falling = 2,
    }

    /// <summary>
    /// Pure rules for the alarm's light and pressure trend, kept off the NetworkBehaviour so EditMode tests reach them.
    /// </summary>
    public static class AirAlarmIndicatorRules
    {
        /// <summary>Smoothed kPa/s a room must move by before the alarm calls it rising or falling.</summary>
        public const float TrendEnterKpaPerSecond = 0.5f;

        /// <summary>Smoothed kPa/s below which a rising or falling room counts as settled again.</summary>
        public const float TrendExitKpaPerSecond = 0.2f;

        /// <summary>Weight of the newest tick in the smoothed pressure rate.</summary>
        public const float RateSmoothing = 0.3f;

        /// <summary>Flags a refill cures. Every other flag is danger whatever the pressure does.</summary>
        public const AirAlarmState RecoverableFlags = AirAlarmState.LowPressure | AirAlarmState.LowOxygen;

        /// <summary>The new smoothed rate after a tick that moved pressure from <paramref name="previousKpa"/> to <paramref name="currentKpa"/>.</summary>
        public static float SmoothRate(float smoothedKpaPerSecond, float previousKpa, float currentKpa, float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                return smoothedKpaPerSecond;

            float rate = (currentKpa - previousKpa) / deltaSeconds;
            return smoothedKpaPerSecond + (rate - smoothedKpaPerSecond) * RateSmoothing;
        }

        /// <summary>The trend for <paramref name="smoothedKpaPerSecond"/>, with hysteresis so a settling room does not flicker.</summary>
        public static AirAlarmPressureTrend ResolveTrend(float smoothedKpaPerSecond, AirAlarmPressureTrend previous)
        {
            float enter = TrendEnterKpaPerSecond;
            float exit = TrendExitKpaPerSecond;
            if (smoothedKpaPerSecond >= (previous == AirAlarmPressureTrend.Rising ? exit : enter))
                return AirAlarmPressureTrend.Rising;

            if (smoothedKpaPerSecond <= -(previous == AirAlarmPressureTrend.Falling ? exit : enter))
                return AirAlarmPressureTrend.Falling;

            return AirAlarmPressureTrend.Steady;
        }

        /// <summary>
        /// Toxic, hot or over-pressure air is danger. Otherwise falling pressure is a leak, and low air that is rising
        /// is a refill; low air that is not moving is danger.
        /// </summary>
        public static AirAlarmIndicator Resolve(bool powered, AirAlarmState state, AirAlarmPressureTrend trend)
        {
            if (!powered)
                return AirAlarmIndicator.Off;

            if ((state & ~RecoverableFlags) != 0)
                return AirAlarmIndicator.Danger;

            if (trend == AirAlarmPressureTrend.Falling)
                return AirAlarmIndicator.Depressurizing;

            if (state == AirAlarmState.None)
                return AirAlarmIndicator.Nominal;

            return trend == AirAlarmPressureTrend.Rising ? AirAlarmIndicator.Repressurizing : AirAlarmIndicator.Danger;
        }
    }
}
