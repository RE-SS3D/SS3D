namespace SS3D.Interactions
{
    /// <summary>
    /// Defines how an interaction resolves when selected from the radial menu.
    /// </summary>
    public enum InteractionTier
    {
        /// <summary>Resolves immediately on the current target.</summary>
        Instant = 1,

        /// <summary>Arms the cursor to pick a world target (e.g. weld).</summary>
        Targeted = 2,

        /// <summary>Arms the cursor for a compatible combine, or drag-and-drop.</summary>
        Combine = 3,
    }
}
