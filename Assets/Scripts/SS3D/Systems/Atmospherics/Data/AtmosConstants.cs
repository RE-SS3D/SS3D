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

        public static readonly GasId Oxygen = new(0);
        public static readonly GasId Nitrogen = new(1);
        public static readonly GasId CarbonDioxide = new(2);
        public static readonly GasId Plasma = new(3);
        public static readonly GasId WaterVapor = new(4);
        public static readonly GasId NitrousOxide = new(5);
        public static readonly GasId Bz = new(6);
        public static readonly GasId Tritium = new(7);
        public static readonly GasId Freon = new(8);
        public static readonly GasId Hydrogen = new(9);
        public static readonly GasId Helium = new(10);
        public static readonly GasId Ammonia = new(11);
        public static readonly GasId Chlorine = new(12);
        public static readonly GasId Fluorine = new(13);
        public static readonly GasId Miasma = new(14);
        public static readonly GasId Ozone = new(15);
        public static readonly GasId ProtoNitrate = new(16);
        public static readonly GasId Zauker = new(17);
        public static readonly GasId HyperNoblium = new(18);
        public static readonly GasId Antinoblium = new(19);

        public const int DefaultGasCount = 20;

        public const int CellsPerChunk = 256;
        public const int ChunkSize = 16;
        public const float CellVolume = 2.5f;
        public const float SpaceTemperature = 173f;
        public const float StationOxygenMoles = 20.79f;
        public const float StationNitrogenMoles = 83.17f;
    }
}
