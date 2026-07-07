namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Notified when the server tilemap mutates. Used by atmospherics, electricity, vision cache, etc.
    /// </summary>
    public interface ITileMutationObserver
    {
        void OnTilePlaced(ITileOccupant occupant, TileCoord coord);

        void OnTileCleared(ITileOccupant occupant, TileCoord coord, TileLayer layer);

        void OnChunkCreated(TileChunkRef chunk);
    }

    /// <summary>
    /// Lightweight reference to a tile chunk for lifecycle observers.
    /// </summary>
    public struct TileChunkRef
    {
        public int MapId;
        public UnityEngine.Vector2Int ChunkKey;
        public UnityEngine.Vector3 Origin;
    }
}
