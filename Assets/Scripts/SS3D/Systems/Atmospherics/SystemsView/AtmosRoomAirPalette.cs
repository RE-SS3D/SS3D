using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.SystemsView
{
    /// <summary>
    /// The Atmos view's Room air colours (creative-mode §4: "tints floors by pressure, which is how you see a vent actually
    /// filling its room"; map-editor-systems-views Phase 5). A floor's pressure is read into one of
    /// <see cref="Levels"/> steps, so the overlay redraws a chunk only when a tile crosses a step, and each step has one
    /// colour: red with no air, amber thin, green about normal (101 kPa), blue then purple above it.
    /// </summary>
    public static class AtmosRoomAirPalette
    {
        /// <summary>Steps between 0 and <see cref="MaxKpa"/>, about 4 kPa each.</summary>
        public const int Levels = 64;

        /// <summary>Pressures at or above this read as the last step.</summary>
        public const float MaxKpa = 252f;

        public const string LegendText =
            "Room air (the button under the toolbar): floors tint by pressure. Red is no air, amber thin, green normal (about 101 kPa), blue and purple above. Off by default.";

        private static readonly float[] StopKpa = { 0f, 45f, 80f, 101.3f, 150f, MaxKpa };

        private static readonly Color[] StopColors =
        {
            new(0.86f, 0.16f, 0.14f),
            new(0.95f, 0.55f, 0.12f),
            new(0.93f, 0.85f, 0.22f),
            new(0.25f, 0.80f, 0.36f),
            new(0.22f, 0.58f, 0.95f),
            new(0.74f, 0.32f, 0.92f),
        };

        public static readonly SystemsViewLegendEntry Legend =
            new(LegendText, ColorFor(0f), ColorFor(60f), ColorFor(101.3f), ColorFor(200f));

        /// <summary>The step a pressure falls in, 0 to <see cref="Levels"/> − 1.</summary>
        public static int LevelOf(float kpa)
        {
            if (float.IsNaN(kpa) || kpa <= 0f)
                return 0;

            return Mathf.Clamp(Mathf.FloorToInt(kpa / MaxKpa * (Levels - 1)), 0, Levels - 1);
        }

        /// <summary>A step's colour: the ramp at the middle of the step.</summary>
        public static Color ColorOfLevel(int level) => ColorFor((Mathf.Clamp(level, 0, Levels - 1) + 0.5f) * MaxKpa / (Levels - 1));

        /// <summary>The ramp, opaque, at <paramref name="kpa"/>.</summary>
        public static Color ColorFor(float kpa)
        {
            if (float.IsNaN(kpa) || kpa <= StopKpa[0])
                return StopColors[0];

            for (int i = 1; i < StopKpa.Length; i++)
            {
                if (kpa <= StopKpa[i])
                    return Color.Lerp(StopColors[i - 1], StopColors[i], (kpa - StopKpa[i - 1]) / (StopKpa[i] - StopKpa[i - 1]));
            }

            return StopColors[StopColors.Length - 1];
        }
    }
}
