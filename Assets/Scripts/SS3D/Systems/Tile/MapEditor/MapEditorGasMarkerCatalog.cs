using SS3D.Core;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Tile.GasMarkers;
using System;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Stable catalog keys for the gas marker library entries (<c>gas-marker:…</c>), and how markers read in the editor.
    /// </summary>
    public static class MapEditorGasMarkerCatalog
    {
        public const string Prefix = "gas-marker:";

        public static string Encode(GasMarkerKind kind) => $"{Prefix}{kind}";

        public static bool TryDecode(string assetName, out GasMarkerKind kind)
        {
            kind = default;
            return !string.IsNullOrEmpty(assetName)
                && assetName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse(assetName.Substring(Prefix.Length), ignoreCase: true, out kind);
        }

        public static string DisplayName(GasMarkerKind kind) => kind switch
        {
            GasMarkerKind.LineFill => "Line fill",
            GasMarkerKind.RoomAir => "Room air",
            _ => "Gas source",
        };

        public static Color PinColor(GasMarkerKind kind) => kind switch
        {
            GasMarkerKind.LineFill => new Color(1f, 0.75f, 0.2f, 0.95f),
            GasMarkerKind.RoomAir => new Color(0.7f, 0.5f, 1f, 0.95f),
            _ => new Color(0.3f, 0.9f, 0.45f, 0.95f),
        };

        public static string PinLabel(GasMarkerRecord record)
        {
            string mix = MixName(record.GasFractions);
            return AtmosGasMix.IsVacuum(record.GasFractions)
                ? $"{DisplayName(record.Kind)}: {mix}"
                : $"{DisplayName(record.Kind)}: {mix}, {record.PressureKpa:0} kPa";
        }

        /// <summary>Vacuum, Air or a single gas by name; anything else is Custom.</summary>
        public static string MixName(float[] fractions)
        {
            if (AtmosGasMix.IsVacuum(fractions))
                return "Vacuum";

            if (AtmosGasMix.Matches(fractions, AtmosGasMix.Air()))
                return "Air";

            if (SubSystems.TryGet(out AtmosSubSystem atmos) && atmos.GasRegistry != null)
            {
                foreach (GasDefinition gas in atmos.GasRegistry.Definitions)
                {
                    if (AtmosGasMix.Matches(fractions, AtmosGasMix.Pure(gas.GasId)))
                        return gas.DisplayName;
                }
            }

            return "Custom";
        }
    }
}
