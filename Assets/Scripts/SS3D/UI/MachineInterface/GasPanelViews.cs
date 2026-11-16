using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>A coloured length of a gas mix bar.</summary>
    public readonly struct GasMixSegment
    {
        public readonly float Percent;

        public readonly Color Color;

        public GasMixSegment(float percent, Color color)
        {
            Percent = percent;
            Color = color;
        }
    }

    /// <summary>One port of a filter or mixer: its pressure and what its gas is made of.</summary>
    public sealed class GasPortView
    {
        public string Label { get; set; } = "Port";

        public string PressureText { get; set; } = "0.0 kPa";

        public StatusTone PressureTone { get; set; } = StatusTone.Neutral;

        public bool Warn { get; set; }

        public List<GasMixSegment> Segments { get; set; } = new();

        public string MixText { get; set; } = "No gas";

        public StatusTone MixTone { get; set; } = StatusTone.Neutral;

        public bool HasCapacity { get; set; }

        public string CapacityText { get; set; } = string.Empty;

        public float CapacityPercent { get; set; }

        public bool CapacityFull { get; set; }
    }

    /// <summary>One gas row on a filter panel.</summary>
    public sealed class GasFilterRowView
    {
        public byte Id { get; set; }

        public string Symbol { get; set; }

        public string Name { get; set; }

        public Color Color { get; set; }

        public bool Selected { get; set; }

        public string PercentText { get; set; }

        public bool Present { get; set; }
    }

    /// <summary>One gas row on a meter panel.</summary>
    public sealed class GasCompositionRowView
    {
        public string Symbol { get; set; }

        public string Name { get; set; }

        public Color Color { get; set; }

        public string PercentText { get; set; }

        public string PressureText { get; set; }

        public bool Present { get; set; }

        public bool Hazard { get; set; }

        public float Percent { get; set; }
    }

    /// <summary>
    /// Text and bar segments for a gas mix, shared by every panel that reads a line.
    /// </summary>
    public static class GasMixDescriber
    {
        private const int MixTextGases = 3;

        /// <summary>
        /// Fills <paramref name="segments"/> richest gas first and returns text naming the top three.
        /// </summary>
        /// <param name="include">Which rows count, or null for all. Shares are out of what's included.</param>
        public static string Describe(
            GasPanelRow[] rows,
            float[] fractions,
            List<GasMixSegment> segments,
            Func<GasPanelRow, bool> include = null)
        {
            segments.Clear();
            if (rows == null || fractions == null)
            {
                return string.Empty;
            }

            var order = new List<int>(rows.Length);
            float total = 0f;
            for (int i = 0; i < rows.Length && i < fractions.Length; i++)
            {
                if (fractions[i] > 0f && (include == null || include(rows[i])))
                {
                    order.Add(i);
                    total += fractions[i];
                }
            }

            if (total <= 0f)
            {
                return string.Empty;
            }

            order.Sort((a, b) => fractions[b].CompareTo(fractions[a]));
            var parts = new List<string>(MixTextGases);
            foreach (int index in order)
            {
                float percent = fractions[index] / total * 100f;
                segments.Add(new GasMixSegment(percent, rows[index].Color));
                if (parts.Count < MixTextGases)
                {
                    parts.Add($"{rows[index].Symbol} {percent:F1}%");
                }
            }

            return string.Join(" · ", parts);
        }
    }
}
