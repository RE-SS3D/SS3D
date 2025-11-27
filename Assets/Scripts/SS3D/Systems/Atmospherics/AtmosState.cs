namespace SS3D.Systems.Atmospherics
{
    public enum AtmosState
    {
        Active = 0,     // Tile is active; equalizes pressures, temperatures and mixes gasses
        Semiactive = 1, // No pressure equalization, but mixes gasses
        Inactive = 2,   // Do nothing
        Vacuum = 3,     // Drain other tiles
        Blocked = 4,    // Wall, skips calculations
    }
}
