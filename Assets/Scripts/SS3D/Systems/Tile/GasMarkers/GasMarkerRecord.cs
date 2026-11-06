using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using System;
using UnityEngine;

namespace SS3D.Systems.Tile.GasMarkers
{
    /// <summary>
    /// One authored gas marker on a station map, saved as JSON and sent over the network as it stands.
    /// At most one marker per tile (see <see cref="GasMarkerRegistry"/>). Treat
    /// <see cref="GasFractions"/> as immutable: undo history and the registry share the array.
    /// </summary>
    [Serializable]
    public struct GasMarkerRecord
    {
        /// <summary>The highest pressure the editor accepts (kPa); ten times a pump's limit is not a chamber.</summary>
        public const float MaxPressureKpa = 10000f;

        public GasMarkerKind Kind;
        public Vector3 Position;

        /// <summary>The pipe line a <see cref="GasMarkerKind.LineFill"/> fills; other kinds ignore it.</summary>
        public TileLayer Line;

        public float PressureKpa;

        /// <summary>Relative amounts indexed by gas id. They need not sum to one; all zero means no gas.</summary>
        public float[] GasFractions;

        public static GasMarkerRecord Default(GasMarkerKind kind) => kind switch
        {
            GasMarkerKind.LineFill => new GasMarkerRecord
            {
                Kind = kind,
                Line = TileLayer.PipeLeft,
                PressureKpa = AtmosPipeConstants.LineFillPressureKpa,
                GasFractions = AtmosGasMix.Air(),
            },
            GasMarkerKind.RoomAir => new GasMarkerRecord
            {
                Kind = kind,
                PressureKpa = AtmosConstants.StandardPressure,
                GasFractions = AtmosGasMix.Vacuum(),
            },
            _ => new GasMarkerRecord
            {
                Kind = kind,
                PressureKpa = AtmosPipeConstants.ChamberPressureKpa,
                GasFractions = AtmosGasMix.Air(),
            },
        };

        /// <summary>Whether this came from a client in a shape the simulation can take.</summary>
        public bool IsValid
        {
            get
            {
                if (!Enum.IsDefined(typeof(GasMarkerKind), Kind) || float.IsNaN(PressureKpa)
                    || PressureKpa is < 0f or > MaxPressureKpa || GasFractions == null
                    || GasFractions.Length > AtmosConstants.MaxGasTypes)
                    return false;

                if (Kind == GasMarkerKind.LineFill && !IsPipeLine(Line))
                    return false;

                foreach (float fraction in GasFractions)
                {
                    if (float.IsNaN(fraction) || float.IsInfinity(fraction) || fraction < 0f)
                        return false;
                }

                return true;
            }
        }

        private static bool IsPipeLine(TileLayer layer) =>
            layer is TileLayer.PipeLeft or TileLayer.PipeMiddle or TileLayer.PipeRight or TileLayer.PipeSurface;
    }

    [Serializable]
    public sealed class SavedGasMarkerChunkPayload
    {
        public GasMarkerRecord[] records;
    }
}
