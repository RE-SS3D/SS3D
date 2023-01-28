using SS3D.Core;
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
        
        public int ChunkCount => _chunks.Count;

        private Dictionary<Vector2Int, TileChunk> _chunks;
        private string _mapName;

        public static TileMap Create(string name)
        {
            GameObject mapObject = new GameObject(name);

            TileMap map = mapObject.AddComponent<TileMap>();
            map.Setup(name);

            return map;
        }

        private void Setup(string mapName)
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
            int x = (int)Math.Floor(worldPosition.x / TileChunk.ChunkSize);
            int y = (int)Math.Floor(worldPosition.z / TileChunk.ChunkSize);

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
            TileChunk chunk = TileChunk.Create(chunkKey, origin);
            chunk.transform.SetParent(transform);

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
                Vector3 origin = new Vector3 { x = key.x * TileChunk.ChunkSize, z = key.y * TileChunk.ChunkSize };
                chunk = CreateChunk(key, origin);
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

        private TileObject GetTileObject(TileLayer layer, Vector3 worldPosition)
        {
            TileChunk chunk = GetOrCreateChunk(worldPosition); // TODO: creates unnessary empty chunk when checking whether building can be done
            return chunk.GetTileObject(layer, worldPosition);
        }


        private TileObject[] GetTileObjects(Vector3 worldPosition)
        {
            TileObject[] tileObjects = new TileObject[TileHelper.GetTileLayerNames().Length];

            foreach (TileLayer layer in TileHelper.GetTileLayerNames())
            {
                tileObjects[(int)layer] = GetTileObject(layer, worldPosition);
            }

            return tileObjects;
        }

        private PlacedTileObject[] GetNeighbourPlacedObjects(TileLayer layer, Vector3 worldPosition)
        {
            PlacedTileObject[] adjacentObjects = new PlacedTileObject[8];

            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++)
            {
                Tuple<int, int> vector = TileHelper.ToCardinalVector(direction);
                adjacentObjects[(int)direction] = GetTileObject(layer, worldPosition + new Vector3(vector.Item1, 0, vector.Item2)).GetPlacedObject();
            }

            return adjacentObjects;
        }

        /// <summary>
        /// Returns whether the specified object can be successfully build for a given position and direction.
        /// </summary>
        /// <param name="tileObjectSo">Object to place</param>
        /// <param name="placePosition">World position to place the object</param>
        /// <param name="dir">Direction the object is facing</param>
        /// <returns></returns>
        public bool CanBuild(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir)
        {
            // Get the right chunk
            TileChunk chunk = GetChunk(placePosition);
            if (chunk == null)
            {
                return true;
            }

            List<Vector2Int> gridPositionList = tileObjectSo.GetGridOffsetList(dir);

            bool canBuild = true;
            foreach (Vector2Int gridOffset in gridPositionList)
            {
                // Verify if we are allowed to build for this grid position
                Vector3 gridPosition = new(placePosition.x + gridOffset.x, 0, placePosition.z + gridOffset.y);

                canBuild &= BuildChecker.CanBuild(GetTileObjects(gridPosition), tileObjectSo);
            }

            // TODO: Add wall mounts colliding into wall check

            return canBuild;
        }

        public bool PlaceTileObject(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir, bool skipBuildCheck)
        {
            bool canBuild = CanBuild(tileObjectSo, placePosition, dir);

            if (canBuild || skipBuildCheck)
            {
                TileChunk chunk = GetOrCreateChunk(placePosition);
                Vector2Int origin = chunk.GetXY(placePosition);
                PlacedTileObject placedObject = PlacedTileObject.Create(placePosition, origin, dir, tileObjectSo);
                placedObject.transform.SetParent(chunk.transform);

                foreach (Vector2Int gridOffset in tileObjectSo.GetGridOffsetList(dir))
                {
                    Vector3 gridPosition = new(placePosition.x + gridOffset.x, 0, placePosition.z + gridOffset.y);
                    chunk = GetOrCreateChunk(gridPosition);

                    chunk.GetTileObject(tileObjectSo.layer, gridPosition).SetPlacedObject(placedObject);
                }

                // Handle Adjacency connectors
                var neighbourTiles = GetNeighbourPlacedObjects(tileObjectSo.layer, placePosition);
                placedObject.UpdateAdjacencies(neighbourTiles);
            }

            return canBuild;
        }

        public void ClearTileObject(Vector3 placePosition, TileLayer layer)
        {
            TileObject[] tileObjects = GetTileObjects(placePosition);
            tileObjects[(int)layer].ClearPlacedObject();

            // Remove any invalid tile combinations
            List<TileObject> toRemoveObjects = BuildChecker.GetToBeDestroyedObjects(tileObjects);

            foreach (TileObject removeObject in toRemoveObjects)
            {
                removeObject.ClearPlacedObject();
            }
        }

        public void PlaceItemObject(Vector3 worldPosition, Quaternion rotation, ItemObjectSo itemObjectSo)
        {
            PlacedItemObject placedItem = PlacedItemObject.Create(worldPosition, rotation, itemObjectSo);
        }

        private void Clear()
        {
            foreach (TileChunk chunk in _chunks.Values)
            {
                chunk.Clear();
            }
        }

        /// <summary>
        /// Returns a new SaveObject for storing the entire map.
        /// </summary>
        /// <returns></returns>
        public MapSaveObject Save()
        {
            List<TileChunk.ChunkSaveObject> chunkObjectSaveList = new List<TileChunk.ChunkSaveObject>();

            foreach (TileChunk chunk in _chunks.Values)
            {
                chunkObjectSaveList.Add(chunk.Save());
            }

            return new MapSaveObject
            {
                mapName = _mapName,
                saveObjectList = chunkObjectSaveList.ToArray(),
            };
        }

        public void Load(MapSaveObject saveObject)
        {
            Clear();

            TileSystem tileSystem = SystemLocator.Get<TileSystem>();

            foreach (var savedChunk in saveObject.saveObjectList)
            {
                TileChunk chunk = GetOrCreateChunk(savedChunk.originPosition);

                foreach (var savedTile in savedChunk.tileObjectSaveObjectArray)
                {
                    TileObjectSo toBePlaced = tileSystem.GetTileAsset(savedTile.placedSaveObject.tileObjectSOName);
                    Vector3 placePosition = chunk.GetWorldPosition(savedTile.x, savedTile.y);

                    // Skipping build check here to allow loading tile objects in a non-valid order
                    PlaceTileObject(toBePlaced, placePosition, savedTile.placedSaveObject.dir, true);
                }
            }
        }
    }
}