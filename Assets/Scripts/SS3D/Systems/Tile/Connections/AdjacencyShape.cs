namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Used for keeping track of different possible adjacency shapes
    /// </summary>
    public enum AdjacencyShape
    {
        //Simple
        O,
        U,
        I,
        L,
        T,
        X,
        //Complex
        LNone,
        LSingle,
        TNone,
        TSingleLeft,
        TSingleRight,
        TDouble,
        XNone,
        XSingle,
        XOpposite,
        XSide,
        XTriple,
        XQuad,
        //Offset
        UNorth,
        USouth,
        LNorthEast,
        LNorthWest,
        LSouthEast,
        LSouthWest,
        TNorthEastWest,
        TNorthSouthWest,
        TNorthSouthEast,
        TSouthWestEast,
        //Vertical
        Vertical,
        // Directional
        LIn,
        LOut,
        ULeft,
        URight,
    }
}