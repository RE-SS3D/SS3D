namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Stable identifier for a gas type in the atmos SoA buffers.
    /// </summary>
    public readonly struct GasId
    {
        public readonly ushort Value;

        public GasId(ushort value) => Value = value;

        public override string ToString() => Value.ToString();
    }

    public static class AtmosConstants
    {
        public const int MaxGasTypes = 64;
        public const float TickInterval = 0.2f;
        public const float GasConstant = 8.314f;
        public const float StandardTemperature = 293.15f;
        public const float StandardPressure = 101.325f;

        // Core SS13 gases. IDs are stable buffer slots; more gases can be appended later.
        public static readonly GasId Oxygen = new(0);
        public static readonly GasId Nitrogen = new(1);
        public static readonly GasId CarbonDioxide = new(2);
        public static readonly GasId Plasma = new(3);

        public const int DefaultGasCount = 4;

        public const int CellsPerChunk = 256;
        public const int ChunkSize = 16;
        public const float CellVolume = 2.5f;
        public const float SpaceTemperature = 173f;
        public const float StationOxygenMoles = 20.79f;
        public const float StationNitrogenMoles = 83.17f;
    }
}
