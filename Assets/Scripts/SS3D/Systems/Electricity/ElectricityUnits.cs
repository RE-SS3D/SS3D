namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Converts between per-tick power flow (kW) and stored energy (kWh).
    /// </summary>
    public static class ElectricityUnits
    {
        public const float DefaultTickSeconds = 0.2f;

        public static float TickSecondsToHours(float tickSeconds) => tickSeconds / 3600f;

        public static float KwToKwh(float kw, float tickSeconds = DefaultTickSeconds) =>
            kw * TickSecondsToHours(tickSeconds);

        public static float KwhToKw(float kwh, float tickSeconds = DefaultTickSeconds)
        {
            if (tickSeconds <= 0f)
            {
                return 0f;
            }

            return kwh / TickSecondsToHours(tickSeconds);
        }
    }
}
