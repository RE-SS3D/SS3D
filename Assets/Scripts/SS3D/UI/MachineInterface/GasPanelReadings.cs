using SS3D.Core;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// One gas as machine panels show it. Sent in each snapshot, so a client needs no gas registry
    /// and a new gas appears on every panel with no UI change.
    /// </summary>
    public struct GasPanelRow
    {
        public byte Id;

        public string Symbol;

        public string Name;

        public Color32 Color;

        public bool Toxic;
    }

    /// <summary>
    /// What a line holds: its pressure and each gas's share, in <see cref="GasPanelRow"/> order.
    /// </summary>
    public struct GasLineReading
    {
        public float PressureKpa;

        public float[] Fractions;
    }

    /// <summary>
    /// Server side: turns pipe networks into what snapshots carry.
    /// </summary>
    public static class GasPanelReadings
    {
        public static GasPanelRow[] BuildRows()
        {
            GasRegistry registry = SubSystems.TryGet(out AtmosSubSystem atmos) ? atmos.GasRegistry : null;
            return registry != null ? BuildRows(registry.Definitions) : BuildDefaultRows();
        }

        public static GasPanelRow[] BuildRows(IReadOnlyList<GasDefinition> definitions)
        {
            var rows = new GasPanelRow[definitions.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                GasDefinition definition = definitions[i];
                rows[i] = new GasPanelRow
                {
                    Id = (byte)definition.Id,
                    Symbol = definition.Symbol,
                    Name = definition.DisplayName,
                    Color = definition.PanelColor,
                    Toxic = definition.IsToxic,
                };
            }

            return rows;
        }

        public static GasLineReading Read(GasPipeNetworkRecord line, GasPanelRow[] rows)
        {
            var reading = new GasLineReading { Fractions = new float[rows.Length] };
            if (line == null)
            {
                return reading;
            }

            reading.PressureKpa = line.GetPressure(AtmosConstants.DefaultGasCount);
            float total = line.GetTotalMoles(AtmosConstants.DefaultGasCount);
            if (total <= 0f)
            {
                return reading;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                int gasId = rows[i].Id;
                reading.Fractions[i] = gasId < line.Moles.Length ? line.Moles[gasId] / total : 0f;
            }

            return reading;
        }

        private static GasPanelRow[] BuildDefaultRows()
        {
            GasDefault[] core = GasDefaults.Core;
            var rows = new GasPanelRow[core.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = new GasPanelRow
                {
                    Id = (byte)core[i].Id,
                    Symbol = core[i].Symbol,
                    Name = core[i].DisplayName,
                    Color = core[i].PanelColor,
                    Toxic = core[i].Toxic,
                };
            }

            return rows;
        }
    }
}
