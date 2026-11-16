namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Rendering-layer bits for URP DecalProjector filtering (requires Decal Renderer
    /// Feature "Use Rendering Layers"). Floor blood projectors target
    /// <see cref="ReceiveWorldDecals"/> only so they never paint characters, who stay
    /// on Default for lighting (<c>m_LightLayerMask: 1</c>).
    /// </summary>
    public static class DecalRenderingLayers
    {
        public const uint DefaultLayer = 1u << 0;
        public const uint ReceiveWorldDecals = 1u << 1;

        /// <summary>Mask for floor/world DecalProjectors.</summary>
        public const uint WorldFloorProjectorMask = ReceiveWorldDecals;

        /// <summary>Mask for body wound DecalProjectors (characters stay on Default).</summary>
        public const uint CharacterProjectorMask = DefaultLayer;

        public static uint WithWorldDecals(uint existing) => existing | ReceiveWorldDecals;
    }
}
