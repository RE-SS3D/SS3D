using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// ScriptableObject that should be used for every tile object.
    /// </summary>
    [CreateAssetMenu(fileName = "TileObjectSo", menuName = "TileMap/TileObjectSo", order = 0)]
    public class TileObjectSo : GenericObjectSo
    {
        [FormerlySerializedAs("layer")]
        [SerializeField]
        private TileLayer _layer;

        [FormerlySerializedAs("genericType")]
        [Tooltip("Specify the generic type. Used for finding matching adjacencies.")]
        [SerializeField]
        private TileObjectGenericType _genericType;

        [FormerlySerializedAs("specificType")]
        [Tooltip("Specify the specific type. Used for setting which generics can connect (e.g. wooden tables only connect to each other).")]
        [SerializeField]
        private TileObjectSpecificType _specificType;

        // Dimensions that the object should use
        [FormerlySerializedAs("width")]
        [SerializeField]
        private int _width = 1;

        [FormerlySerializedAs("height")]
        [SerializeField]
        private int _height = 1;

        // Whether or not a wall mount is large
        [FormerlySerializedAs("isLarge")]
        [SerializeField]
        private bool _isLarge;

        public TileLayer Layer => _layer;

        public TileObjectGenericType GenericType => _genericType;

        public TileObjectSpecificType SpecificType => _specificType;

        public bool IsLarge => _isLarge;

        public int Width => _width;

        public int Height => _height;

        /// <summary>
        /// TODO : document and understand properly this method. Why the diagonal directions are not treated ?
        /// </summary>
        public List<Vector2Int> GetGridOffsetList(Direction dir)
        {
            List<Vector2Int> gridOffsetList = new();

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    switch (dir)
                    {
                        case Direction.South:
                        {
                            gridOffsetList.Add(Vector2Int.zero + new Vector2Int(x, y));
                            break;
                        }

                        case Direction.North:
                        {
                            gridOffsetList.Add(Vector2Int.zero - new Vector2Int(x, y));
                            break;
                        }

                        case Direction.West:
                        {
                            gridOffsetList.Add(Vector2Int.zero - new Vector2Int(-x, y));
                            break;
                        }

                        case Direction.East:
                        {
                            gridOffsetList.Add(Vector2Int.zero + new Vector2Int(-x, y));
                            break;
                        }

                        // This probably doesn't work for tilemap objects bigger than a single tile
                        // and facing a diagonal direction. TODO, properly do it for those cases.
                        default:
                        {
                            gridOffsetList.Add(Vector2Int.zero);
                            break;
                        }
                    }
                }
            }

            return gridOffsetList;
        }

        public void Init(int width, int height, TileLayer layer, TileObjectGenericType genericType)
        {
            _width = width;
            _height = height;
            _layer = layer;
            _genericType = genericType;
        }
    }
}
