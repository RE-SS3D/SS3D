using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Class used for storing and modifying a tile map.
    /// </summary>
    public class TileMap : MonoBehaviour
    {
        /// <summary>
        /// Save object used for reconstructing a tilemap.
        /// </summary>
        [Serializable]
        public class MapSaveObject
        {
            public string mapName;
            public TileChunk.ChunkSaveObject[] saveObjectList;
        }

        /// <summary>
        /// Number of TileObjects that should go in a chunk. 16 x 16
        /// </summary>
        private const int ChunkSize = 16;

        /// <summary>
        /// The size of each tile.
        /// </summary>
        private const float TileSize = 1.0f;

        
        public int ChunkCount => _chunks.Count;

        private Dictionary<Vector2Int, TileChunk> _chunks;
        private string _mapName;

        /// <summary>
        /// Creates a new TileMap.
        /// </summary>
        /// <param name="name">Name of the map</param>
        /// <returns></returns>
        public static TileMap Create(string name)
        {
            GameObject mapObject = new GameObject(name);

            TileMap map = mapObject.AddComponent<TileMap>();
            map.Setup(name);

            return map;
        }

        /// <summary>
        /// Initialize the new map.
        /// </summary>
        /// <param name="mapName"></param>
        public void Setup(string mapName)
        {
            _chunks = new Dictionary<Vector2Int, TileChunk>();
            // _tileSystem = SystemLocator.Get<TileSystem>();
            name = mapName;
            _mapName = mapName;
        }

        /// <summary>
        /// Returns the chunk key to be used based on a world position.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public Vector2Int GetKey(Vector3 worldPosition)
        {
            int x = (int)Math.Floor(worldPosition.x / ChunkSize);
            int y = (int)Math.Floor(worldPosition.z / ChunkSize);

            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Create a new chunk. Internal use only
        /// </summary>
        /// <param name="chunkKey">Unique key to use</param>
        /// <param name="origin">Origin position of the chunk</param>
        /// <returns></returns>
        private TileChunk CreateChunk(Vector2Int chunkKey, Vector3 origin)
        {
            TileChunk chunk = new TileChunk(chunkKey, ChunkSize, ChunkSize, TileSize, origin);
            return chunk;
        }

        /// <summary>
        /// Returns chunk based the world position. Will create a new chunk if it doesn't exist.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        private TileChunk GetOrCreateChunk(Vector3 worldPosition)
        {
            TileChunk chunk = GetChunk(worldPosition);
            if (chunk == null)
            {
                Vector2Int key = GetKey(worldPosition);
                Vector3 origin = new Vector3 { x = key.x * ChunkSize, z = key.y * ChunkSize };
                _ = CreateChunk(key, origin);
                _chunks[key] = chunk;
            }
            return chunk;
        }

        private TileChunk GetChunk(Vector3 worldPosition)
        {
            Vector2Int key = GetKey(worldPosition);

            if (_chunks.TryGetValue(key, out TileChunk _))
            {
                return _chunks[key];
            }
            else
            {
                return null;
            }
        }

        public TileObject GetTileObject(TileLayer layer, Vector3 worldPosition)
        {
            TileChunk chunk = GetOrCreateChunk(worldPosition); // TODO: creates unnessary empty chunk when checking whether building can be done
            return chunk.GetTileObject(layer, worldPosition);
        }


        public TileObject[] GetTileObjects(Vector3 worldPosition)
        {
            TileObject[] tileObjects = new TileObject[TileHelper.GetTileLayerNames().Length];

            foreach (TileLayer layer in TileHelper.GetTileLayerNames())
            {
                tileObjects[(int)layer] = GetTileObject(layer, worldPosition);
            }

            return tileObjects;
        }

    /// <summary>
    /// Returns whether the specified object can be successfully build for a given position and direction.
    /// </summary>
    /// <param name="tileObjectSo">Object to place</param>
    /// <param name="position">World position to place the object</param>
    /// <param name="dir">Direction the object is facing</param>
    /// <returns></returns>
    public bool CanBuild(TileObjectSo tileObjectSo, Vector3 position, Direction dir)
        {
            // Get the right chunk
            TileChunk chunk = GetChunk(position);
            if (chunk == null)
            {
                return true;
            }

            Vector2Int placedObjectOrigin = chunk.GetXY(position);
            TileLayer layer = tileObjectSo.layer;

            List<Vector2Int> gridPositionList = tileObjectSo.GetGridPositionList(placedObjectOrigin, dir);

            bool canBuild = true;
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                // Verify if we are allowed to build for this grid position
                Vector3 checkWorldPosition = chunk.GetWorldPosition(gridPosition.x, gridPosition.y);

                canBuild &= BuildChecker.CanBuild(GetTileObjects(checkWorldPosition), tileObjectSo);

                /*
                if (chunk.GetTileObject(layer, gridPosition.x, gridPosition.y) == null)
                {
                    // We got a chunk edge case in which a multi tile object is outside of the chunk
                    Vector3 offEdgeObjectPosition = chunk.GetWorldPosition(gridPosition.x, gridPosition.y);
                    TileChunk nextChunk = GetChunk(offEdgeObjectPosition);

                    // If neighbour chunk is empty, we are good
                    if (nextChunk == null)
                    {
                        continue;
                    }

                    // Retrieve neighbour chunks x,y offsets and see if it is occupied
                    Vector2Int chunkOffset = nextChunk.GetXY(offEdgeObjectPosition);
                    if (!nextChunk.GetTileObject(layer, chunkOffset.x, chunkOffset.y).IsEmpty())
                    {
                        canBuild = false;
                        break;
                    }
                }
                */
            }

            return canBuild;
        }
    }
}