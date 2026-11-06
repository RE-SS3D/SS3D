using UnityEngine;

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
        public readonly string Symbol;
        public readonly Color32 PanelColor;
        public readonly bool Toxic;

        public GasDefault(
            ushort id,
            string displayName,
            float molarMass,
            float specificHeat,
            string symbol,
            Color32 panelColor,
            bool toxic = false)
        {
            Id = id;
            DisplayName = displayName;
            MolarMass = molarMass;
            SpecificHeat = specificHeat;
            Symbol = symbol;
            PanelColor = panelColor;
            Toxic = toxic;
        }
    }

    public static class GasDefaults
    {
        /// <summary>
        /// Core SS13 gases. Molar mass and specific heat follow the henkhooft tuning values.
        /// </summary>
        public static readonly GasDefault[] Core =
        {
            new(0, "Oxygen", 32f, 2f, "O2", new Color32(111, 159, 216, 255)),
            new(1, "Nitrogen", 28f, 20f, "N2", new Color32(185, 188, 194, 255)),
            new(2, "Carbon Dioxide", 44f, 3f, "CO2", new Color32(125, 131, 139, 255)),
            new(3, "Plasma", 78f, 10f, "Plasma", new Color32(196, 111, 214, 255), toxic: true),
        };

        public static int CoreGasCount => Core.Length;
    }
}
