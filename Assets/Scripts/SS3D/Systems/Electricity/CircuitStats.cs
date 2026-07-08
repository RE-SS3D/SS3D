namespace System.Electricity
{
    public struct CircuitStats
    {
        public float TotalSupplyKw;

        public float TotalDemandKw;

        public float ApcBatteryCharge;

        public float LightingLoadKw;

        public float EquipmentLoadKw;

        public float EnvironmentLoadKw;

        public bool GridMeetsLoad;

        public bool BatteryDraining;
    }
}
