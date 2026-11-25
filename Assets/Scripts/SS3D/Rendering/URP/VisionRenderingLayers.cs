namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Rendering-layer bit for people, items and wall mounts (systems/fov.md People, items, wall mounts). <c>VisionEntityCuller</c>
    /// already decides whether each is in sight, so the vision mask pass shows their pixels instead of cutting
    /// them at the edge of sight. It is the named layer <c>VisionEntity</c> (index 2 in <c>TagManager</c>): URP
    /// sizes the rendering-layers texture by the named layers (8 bits here) and drops unnamed bits.
    /// </summary>
    public static class VisionRenderingLayers
    {
        public const uint Entity = 1u << 2;

        public static uint WithEntity(uint existing) => existing | Entity;
    }
}
