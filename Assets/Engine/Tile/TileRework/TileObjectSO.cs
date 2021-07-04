using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.TilesRework
{
    [CreateAssetMenu(fileName = "TileObjectSO", menuName = "TileMap/TileObjectSO", order = 0)]
    public class TileObjectSO : ScriptableObject
    {
        public TileLayer layerType;
        public string nameString;
        public GameObject prefab;
        public int width = 1;
        public int height = 1;

        public Vector2Int GetRotationOffset(Direction dir)
        {
            switch (dir)
            {
                default:
                case Direction.South: return new Vector2Int(0, 0);
                case Direction.West: return new Vector2Int(0, width - 1);
                case Direction.North: return new Vector2Int(width - 1, height - 1);
                case Direction.East: return new Vector2Int(height - 1, 0);
            }
        }

        public List<Vector2Int> GetGridPositionList(Vector2Int offset, Direction dir)
        {
            List<Vector2Int> gridPositionList = new List<Vector2Int>();
            switch (dir)
            {
                default:
                case Direction.South:
                case Direction.North:
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            gridPositionList.Add(offset + new Vector2Int(x, y));
                        }
                    }
                    break;
                case Direction.West:
                case Direction.East:
                    for (int x = 0; x < height; x++)
                    {
                        for (int y = 0; y < width; y++)
                        {
                            gridPositionList.Add(offset + new Vector2Int(x, y));
                        }
                    }
                    break;
            }
            return gridPositionList;
        }
    }
}