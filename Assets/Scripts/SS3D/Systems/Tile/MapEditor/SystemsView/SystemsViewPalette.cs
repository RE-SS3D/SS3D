using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// The fixed network colours (Q3): a view id indexes this palette, so a network keeps its colour as long as its id.
    /// A network may also name a hue <see cref="SystemsViewNetwork.Family"/>: its id then picks a shade of that hue, so a
    /// domain can match the world's own colours (atmos Supply pipes are blue, Waste red) and still tell networks apart.
    /// </summary>
    public static class SystemsViewPalette
    {
        /// <summary>Drawn for view id 0 (no network).</summary>
        public static readonly Color None = new(0.55f, 0.55f, 0.55f, 1f);

        // Saturated and far apart in hue, so neighbouring networks read as different at a glance.
        private static readonly Color[] Colors =
        {
            new(0.95f, 0.35f, 0.30f, 1f),
            new(0.25f, 0.65f, 0.95f, 1f),
            new(0.40f, 0.85f, 0.35f, 1f),
            new(0.98f, 0.75f, 0.20f, 1f),
            new(0.75f, 0.45f, 0.95f, 1f),
            new(0.20f, 0.85f, 0.80f, 1f),
            new(0.98f, 0.55f, 0.15f, 1f),
            new(0.95f, 0.45f, 0.75f, 1f),
            new(0.65f, 0.80f, 0.25f, 1f),
            new(0.45f, 0.50f, 0.98f, 1f),
            new(0.80f, 0.60f, 0.40f, 1f),
            new(0.60f, 0.95f, 0.65f, 1f),
        };

        /// <summary>No family: the id picks from the whole palette.</summary>
        public const byte AnyHue = 0;
        public const byte Blue = 1;
        public const byte Green = 2;
        public const byte Red = 3;
        public const byte Amber = 4;

        // Per family, shades far enough apart in lightness that two networks of one hue side by side still read as two.
        private static readonly Color[][] Shades =
        {
            null,
            new Color[] { new(0.25f, 0.55f, 0.98f, 1f), new(0.55f, 0.82f, 1f, 1f), new(0.18f, 0.35f, 0.80f, 1f), new(0.35f, 0.90f, 0.98f, 1f) },
            new Color[] { new(0.40f, 0.85f, 0.35f, 1f), new(0.70f, 0.95f, 0.55f, 1f), new(0.20f, 0.62f, 0.32f, 1f), new(0.60f, 0.80f, 0.20f, 1f) },
            new Color[] { new(0.95f, 0.30f, 0.28f, 1f), new(1f, 0.58f, 0.50f, 1f), new(0.72f, 0.16f, 0.22f, 1f), new(0.95f, 0.40f, 0.60f, 1f) },
            new Color[] { new(0.98f, 0.75f, 0.20f, 1f), new(1f, 0.88f, 0.50f, 1f), new(0.88f, 0.55f, 0.12f, 1f), new(0.92f, 0.92f, 0.35f, 1f) },
        };

        public static Color ColorFor(int viewId) => viewId <= 0 ? None : Colors[(viewId - 1) % Colors.Length];

        /// <summary>A network's colour: a shade of <paramref name="family"/>, or the whole palette for <see cref="AnyHue"/> or an unknown family.</summary>
        public static Color ColorFor(int viewId, byte family)
        {
            if (viewId <= 0 || family == AnyHue || family >= Shades.Length)
                return ColorFor(viewId);

            Color[] shades = Shades[family];
            return shades[(viewId - 1) % shades.Length];
        }

        /// <summary>The first shade of each family, for a legend line.</summary>
        public static Color FamilySwatch(byte family) => ColorFor(1, family);
    }
}
