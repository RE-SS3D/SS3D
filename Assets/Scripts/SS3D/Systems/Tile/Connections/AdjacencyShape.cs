namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Used for keeping track of different possible adjacency shapes
    /// </summary>
    public enum AdjacencyShape
    {
        // Simple
        O = 0,
        U = 1,
        I = 2,
        L = 3,
        T = 4,
        X = 5,

        // Complex
        LNone = 6,
        LSingle = 7,
        TNone = 8,
        TSingleLeft = 9,
        TSingleRight = 10,
        TDouble = 11,
        XNone = 12,
        XSingle = 13,
        XOpposite = 14,
        XSide = 15,
        XTriple = 16,
        XQuad = 17,

        // Offset
        UNorth = 18,
        USouth = 19,
        LNorthEast = 20,
        LNorthWest = 21,
        LSouthEast = 22,
        LSouthWest = 23,
        TNorthEastWest = 24,
        TNorthSouthWest = 25,
        TNorthSouthEast = 26,
        TSouthWestEast = 27,

        // Vertical
        Vertical = 28,

        // Directional
        LIn = 29,
        LOut = 30,
        ULeft = 31,
        URight = 32,

        // PipeOffsetMachinery
        INorthMachinery = 33,
        ISouthMachinery = 34,
    }
}
