namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Per-frame vision render state shared between renderer passes and <see cref="Systems.Vision.VisionSubSystem"/>.
    /// </summary>
    public static class VisionRenderContext
    {
        /// <summary>True while the FOV composite should run for the game camera.</summary>
        public static bool Enabled { get; set; }

        /// <summary>
        /// Meta views (map editor) set this so FOV stays off even if the producer is late to start.
        /// </summary>
        public static bool Suppressed { get; set; }
    }
}
