namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Per-frame vision render state shared between renderer passes and <see cref="Systems.Vision.VisionSubSystem"/>.
    /// </summary>
    public static class VisionRenderContext
    {
        public static bool Enabled { get; set; }
    }
}
