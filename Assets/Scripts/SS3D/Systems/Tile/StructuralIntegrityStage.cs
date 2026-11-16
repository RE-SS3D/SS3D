namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Per-tile structural failure stages from explosives-destruction.md §3.
    /// </summary>
    public enum StructuralIntegrityStage : byte
    {
        Intact = 0,
        Damaged = 1,
        Cracked = 2,
        Destroyed = 3,
    }
}
