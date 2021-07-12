using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles
{
    [CreateAssetMenu(fileName = "TileObjectSO", menuName = "TileMap/TileObjectSO", order = 0)]
    public class TileObjectSO : ScriptableObject
    {
        public TileLayer layerType;

        [Tooltip("A name for the object. Make sure it is unique.")]
        public string nameString;

        [Tooltip("Specify the generic type. Used for finding matching adjacencies.")]
        public string genericType;
        public GameObject prefab;
        public int width = 1;
        public int height = 1;

        public Vector2Int GetRotationOffset(Direction dir)
        {
            switch (dir)
            {
                default:
                    return new Vector2Int(0, 0);
            }
        }

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
                            gridPositionList.Add(offset - new Vector2Int(x, y));
                            break;
                        case Direction.North:
                            gridPositionList.Add(offset + new Vector2Int(x, y));
                            break;
                        case Direction.West:
                            gridPositionList.Add(offset + new Vector2Int(-x, y));
                            break;
                        case Direction.East:
                            gridPositionList.Add(offset - new Vector2Int(-x, y));
                            break;
                    }
                }
            }
            return gridPositionList;
        }
    }
}