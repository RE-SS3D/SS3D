namespace SS3D.Systems.Selection
{
    /// <summary>
    /// Reserved rendering-layer bit used to exclude auxiliary renderers (e.g. interaction outline
    /// shells) from the selection colour-pick pass, while still letting them render normally in the
    /// main camera. Keeping these renderers out of the pick pass avoids them being drawn with the
    /// override selection material and z-fighting against the real mesh at silhouette edges, which
    /// caused the hover selection to flicker.
    /// </summary>
    public static class SelectionRenderingLayers
    {
        public const uint ExcludeFromSelectionPick = 1u << 31;
        public const uint PickPassMask = ~ExcludeFromSelectionPick;
    }
}
