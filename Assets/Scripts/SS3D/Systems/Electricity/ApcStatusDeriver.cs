namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Derives APC panel power/battery status from circuit load statistics.
    /// </summary>
    public static class ApcStatusDeriver
    {
        private const float CriticalBatteryThreshold = 0.15f;

        public static ApcPowerState DerivePowerState(CircuitStats stats)
        {
            if (stats.TotalSupplyKw <= 0f && stats.ApcBatteryCharge <= CriticalBatteryThreshold)
            {
                return ApcPowerState.Critical;
            }

            if (stats.ApcBatteryCharge <= CriticalBatteryThreshold && !stats.GridMeetsLoad)
            {
                return ApcPowerState.Critical;
            }

            if (!stats.GridMeetsLoad || stats.BatteryDraining)
            {
                return ApcPowerState.Overload;
            }

            return ApcPowerState.Nominal;
        }

        public static ApcBatteryState DeriveBatteryState(CircuitStats stats)
        {
            if (stats.ApcBatteryCharge <= CriticalBatteryThreshold)
            {
                return ApcBatteryState.Critical;
            }

            if (stats.BatteryDraining)
            {
                return ApcBatteryState.Discharging;
            }

            return ApcBatteryState.Charged;
        }
    }
}
