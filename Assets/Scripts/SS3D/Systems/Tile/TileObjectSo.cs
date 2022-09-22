using System.Collections.Generic;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Engine.Tile.TileRework
{
    /// <summary>
    /// Scriptable object that should be used for every tile object.
    /// </summary>
    [CreateAssetMenu(fileName = "TileObjectSo", menuName = "TileMap/TileObjectSo", order = 0)]
    public class TileObjectSo : ScriptableObject
    {
        public TileLayer layer;
        
        [Tooltip("A name for the object. Make sure it is unique.")]
        public string nameString;
        
        [Tooltip("Specify the generic type. Used for finding matching adjacencies.")]
        public TileObjectGenericType genericType;

        [Tooltip("Specify the specific type. Used for setting which generics can connect (e.g. wooden tables only connect to each other).")]
        public TileObjectSpecificType specificType;

        public GameObject prefab;
        public int width = 1;
        public int height = 1;

        /// <summary>
        /// Returns a rotation offset if the origin of an object needs to be moved.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public Vector2Int GetRotationOffset(Direction dir)
        {
            switch (dir)
            {
                default:
                    return new Vector2Int(0, 0);
            }
        }

        /// <summary>
        /// Returns a list of all positions that the object occupies while taking direction into account.
        /// </summary>
        /// <param name="offset">Origin offset that should be taken into account</param>
        /// <param name="dir">Direction the object is facing</param>
        /// <returns></returns>
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
    }
}