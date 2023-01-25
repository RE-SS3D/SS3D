using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Scriptable object that should be used for every tile object.
    /// </summary>
    [CreateAssetMenu(fileName = "TileObjectSo", menuName = "TileMap/TileObjectSo", order = 0)]
    public class TileObjectSo : ScriptableObject
    {
        [Tooltip("A name for the object. Make sure it is unique.")]
        public string nameString;
        public TileLayer layer;
        public GameObject prefab;

        [Tooltip("Specify the generic type. Used for finding matching adjacencies.")]
        public TileObjectGenericType genericType;

        [Tooltip("Specify the specific type. Used for setting which generics can connect (e.g. wooden tables only connect to each other).")]
        public TileObjectSpecificType specificType;

        // Dimensions that the object should use
        public int width = 1;
        public int height = 1;

        /// <summary>
        /// Returns a list of all positions that the object occupies while taking direction into account.
        /// </summary>
        /// <param name="offset">Origin offset that should be taken into account</param>
        /// <param name="dir">Direction the object is facing</param>
        /// <returns></returns>
        /*
        public List<Vector2Int> GetGridPositionList(Vector2Int offset, Direction dir)
        {
            List<Vector2Int> gridPositionList = new List<Vector2Int>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    switch (dir)
                    {
                        case Direction.South:
                            gridPositionList.Add(offset + new Vector2Int(x, y));
                            break;
                        case Direction.North:
                            gridPositionList.Add(offset - new Vector2Int(x, y));
                            break;
                        case Direction.West:
                            gridPositionList.Add(offset - new Vector2Int(-x, y));
                            break;
                        case Direction.East:
                            gridPositionList.Add(offset + new Vector2Int(-x, y));
                            break;
                    }
                }
            }
            return gridPositionList;
        }
        */

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
                    }
                }
            }
            return gridOffsetList;
        }
    }
}