namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Read-only snapshot of a neighbouring directional used during configuration evaluation.
    /// </summary>
    public readonly struct DirectionalNeighbourState
    {
        public DirectionalNeighbourState(
            PlacedTileObject tile,
            Direction facing,
            AdjacencyShape currentShape,
            int connectionCount,
            PlacedTileObject firstNeighbour,
            PlacedTileObject secondNeighbour)
        {
            Tile = tile;
            Facing = facing;
            CurrentShape = currentShape;
            ConnectionCount = connectionCount;
            FirstNeighbour = firstNeighbour;
            SecondNeighbour = secondNeighbour;
        }

        public PlacedTileObject Tile { get; }

        public Direction Facing { get; }

        public AdjacencyShape CurrentShape { get; }

        public int ConnectionCount { get; }

        public PlacedTileObject FirstNeighbour { get; }

        public PlacedTileObject SecondNeighbour { get; }
    }
}
