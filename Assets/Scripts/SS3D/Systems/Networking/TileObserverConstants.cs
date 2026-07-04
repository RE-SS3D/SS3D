using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Networking
{
    /// <summary>
    /// FishNet HashGrid settings aligned with tile chunk boundaries.
    /// </summary>
    public static class TileObserverConstants
    {
        /// <summary>
        /// HashGrid accuracy in world units (Boot scene NetworkManager HashGrid).
        /// Half-accuracy equals one <see cref="TileConstants.ChunkSize"/> chunk; FishNet's 3×3
        /// neighbor window then covers roughly three chunks per axis (~48m) before pop-in.
        /// </summary>
        public const ushort HashGridAccuracy = (ushort)(TileConstants.ChunkSize * 2);

        /// <summary>
        /// Matches FishNet HashGrid cell math for XZ axes (see Boot scene NetworkManager).
        /// </summary>
        public static Vector2Int GetHashGridCell(Vector3 worldPosition)
        {
            int halfAccuracy = Mathf.CeilToInt(HashGridAccuracy / 2f);
            return new Vector2Int(
                (int)worldPosition.x / halfAccuracy,
                (int)worldPosition.z / halfAccuracy);
        }
    }
}
