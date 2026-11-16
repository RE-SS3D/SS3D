namespace SS3D.Rendering.URP
{
    /// <summary>
    /// What the vision render passes read each frame. Written only by <c>VisionSubSystem</c>, which owns the
    /// vision state (2026-11_fov-rework D6); the rendering assembly can't reference it directly.
    /// </summary>
    public static class VisionRenderContext
    {
        /// <summary>True while the FOV mask should be drawn for the game camera.</summary>
        public static bool Enabled { get; private set; }

        /// <summary>Called by <c>VisionSubSystem</c> when its state changes.</summary>
        public static void Publish(bool enabled) => Enabled = enabled;
    }
}
