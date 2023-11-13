using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// ScriptableObject that should be used for every tile object.
    /// </summary>
    [CreateAssetMenu(fileName = "TileObjectSo", menuName = "TileMap/TileObjectSo", order = 0)]
    public class TileObjectSo : GenericObjectSo
    {
        public TileLayer layer;

        [Tooltip("Specify the generic type. Used for finding matching adjacencies.")]
        public TileObjectGenericType genericType;

        [Tooltip("Specify the specific type. Used for setting which generics can connect (e.g. wooden tables only connect to each other).")]
        public TileObjectSpecificType specificType;

        // Dimensions that the object should use
        public int width = 1;
        public int height = 1;

        /// <summary>
        /// TODO : document and understand properly this method. Why the diagonal directions are not treated ?
        /// </summary>
        public List<Vector2Int> GetGridOffsetList(Direction dir)
        {
            List<Vector2Int> gridOffsetList = new List<Vector2Int>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    switch (dir)
                    {
                        case Direction.South:
                            gridOffsetList.Add(Vector2Int.zero + new Vector2Int(x, y));
                            break;
                        case Direction.North:
                            gridOffsetList.Add(Vector2Int.zero - new Vector2Int(x, y));
                            break;
                        case Direction.West:
                            gridOffsetList.Add(Vector2Int.zero - new Vector2Int(-x, y));
                            break;
                        case Direction.East:
                            gridOffsetList.Add(Vector2Int.zero + new Vector2Int(-x, y));
                            break;

                        // This probably doesn't work for tilemap objects bigger than a single tile
                        // and facing a diagonal direction. TODO, properly do it for those cases.
                        default:
                            gridOffsetList.Add(Vector2Int.zero);
                            break;
                    }
                }
            }
            return gridOffsetList;
        }
    }
}