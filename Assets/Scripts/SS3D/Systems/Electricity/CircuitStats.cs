namespace System.Electricity
{
    public struct CircuitStats
    {
        public float TotalSupplyKw;

        /// <summary>
        /// Grid headroom available to the APC before the last area draw, in kW.
        /// </summary>
        public float GridAvailableKw;

        public float TotalDemandKw;

        public float ApcBatteryCharge;

        public float LightingLoadKw;

        public float EquipmentLoadKw;

        public float EnvironmentLoadKw;

        public bool GridMeetsLoad;

        public bool BatteryDraining;
    }
}
