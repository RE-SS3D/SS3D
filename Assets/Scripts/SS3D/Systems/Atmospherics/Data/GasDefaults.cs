namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Plain metadata for a gas, used both to bootstrap the sim without a registry
    /// and to generate the <see cref="GasRegistry"/> asset.
    /// </summary>
    public readonly struct GasDefault
    {
        public readonly ushort Id;
        public readonly string DisplayName;
        public readonly float MolarMass;
        public readonly float SpecificHeat;

        public GasDefault(ushort id, string displayName, float molarMass, float specificHeat)
        {
            Id = id;
            DisplayName = displayName;
            MolarMass = molarMass;
            SpecificHeat = specificHeat;
        }
    }

    public static class GasDefaults
    {
        /// <summary>
        /// Core SS13 gases. Molar mass and specific heat follow the henkhooft tuning values.
        /// </summary>
        public static readonly GasDefault[] Core =
        {
            new(0, "Oxygen", 32f, 2f),
            new(1, "Nitrogen", 28f, 20f),
            new(2, "Carbon Dioxide", 44f, 3f),
            new(3, "Plasma", 78f, 10f),
        };

        public static int CoreGasCount => Core.Length;
    }
}
