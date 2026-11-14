using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>The fixed network colours (Q3): a view id indexes this palette, so a network keeps its colour as long as its id.</summary>
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

        public static Color ColorFor(int viewId) => viewId <= 0 ? None : Colors[(viewId - 1) % Colors.Length];
    }
}
