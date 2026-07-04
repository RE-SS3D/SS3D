using SS3D.Systems.Tile;

namespace SS3D.Systems.Networking
{
    /// <summary>
    /// FishNet HashGrid settings aligned with tile chunk boundaries.
    /// </summary>
    public static class TileObserverConstants
    {
        /// <summary>
        /// HashGrid accuracy in world units. Matches <see cref="TileConstants.ChunkSize"/>.
        /// </summary>
        public const ushort HashGridAccuracy = TileConstants.ChunkSize;
    }
}
