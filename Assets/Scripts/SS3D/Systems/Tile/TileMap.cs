using FishNet;
using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Class used for storing and modifying a tile map.
    /// </summary>
    public class TileMap : NetworkBehaviour
    {
        private Dictionary<Vector2Int, TileChunk> _chunks;
        private List<PlacedItemObject> _items;
        private string _mapName;

        public int ChunkCount => _chunks.Count;

        public static TileMap Create(string name)
        {
            GameObject mapObject = new GameObject(name);

            TileMap map = mapObject.AddComponent<TileMap>();
            map.Setup(name);

            if (InstanceFinder.ServerManager != null && mapObject.GetComponent<NetworkObject>() != null)
            {
                InstanceFinder.ServerManager.Spawn(mapObject);
            }

            return map;
        }

        private void Setup(string mapName)
        {
            _chunks = new Dictionary<Vector2Int, TileChunk>();
            _items = new List<PlacedItemObject>();
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
            TileObject[] tileObjects = new TileObject[TileHelper.GetTileLayers().Length];

            foreach (TileLayer layer in TileHelper.GetTileLayers())
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
                adjacentObjects[(int)direction] = GetTileObject(layer, worldPosition + new Vector3(vector.Item1, 0, vector.Item2)).PlacedObject;
            }

            return adjacentObjects;
        }

        /// <summary>
        /// Returns whether the specified object can be successfully build for a given position and direction.
        /// </summary>
        /// <param name="tileObjectSo">Object to place</param>
        /// <param name="placePosition">World position to place the object</param>
        /// <param name="dir">Direction the object is facing</param>
        /// <param name="replaceExisting">Replace an existing object</param>
        /// <returns></returns>
        public bool CanBuild(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir, bool replaceExisting)
        {
            // Get the right chunk
            TileChunk chunk = GetOrCreateChunk(placePosition);
            List<Vector2Int> gridPositionList = tileObjectSo.GetGridOffsetList(dir);

            bool canBuild = true;
            foreach (Vector2Int gridOffset in gridPositionList)
            {
                // Verify if we are allowed to build for this grid position
                Vector3 gridPosition = new(placePosition.x + gridOffset.x, 0, placePosition.z + gridOffset.y);

                canBuild &= BuildChecker.CanBuild(GetTileObjects(gridPosition), tileObjectSo, dir, 
                    GetNeighbourPlacedObjects(TileLayer.Turf, gridPosition), replaceExisting);
            }
            
            return canBuild;
        }

        public bool PlaceTileObject(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir, bool skipBuildCheck, bool replaceExisting)
        {
            bool canBuild = CanBuild(tileObjectSo, placePosition, dir, replaceExisting);

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

                    // Remove an existing object if there
                    if (replaceExisting)
                        ClearTileObject(gridPosition, tileObjectSo.layer);

                    // Place new object
                    chunk.GetTileObject(tileObjectSo.layer, gridPosition).PlacedObject = placedObject;
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

            // Update any neighbouring adjacencies
            ResetAdjacencies(placePosition, layer);

            // Remove any invalid tile combinations
            List<TileObject> toRemoveObjects = BuildChecker.GetToBeDestroyedObjects(tileObjects);

            foreach (TileObject removeObject in toRemoveObjects)
            {
                removeObject.ClearPlacedObject();
                ResetAdjacencies(placePosition, removeObject.Layer);
            }
        }

        private void ResetAdjacencies(Vector3 placePosition, TileLayer layer)
        {
            var neighbourTiles = GetNeighbourPlacedObjects(layer, placePosition);
            for (int i = 0; i < neighbourTiles.Length; i++)
            {
                neighbourTiles[i]?.UpdateSingleAdjacency(null, TileHelper.GetOpposite((Direction)i));
            }
        }

        public void PlaceItemObject(Vector3 worldPosition, Quaternion rotation, ItemObjectSo itemObjectSo)
        {
            PlacedItemObject placedItem = PlacedItemObject.Create(worldPosition, rotation, itemObjectSo);
            placedItem.transform.SetParent(transform);
            _items.Add(placedItem);
        }

        public void ClearItemObject(Vector3 worldPosition, ItemObjectSo itemObjectSo)
        {
            List<PlacedItemObject> placedItems = _items.FindAll(item => item.NameString == itemObjectSo.nameString);
            PlacedItemObject toRemove = null;

            foreach (PlacedItemObject item in placedItems)
            {
                if (Vector3.Distance(item.transform.position, worldPosition) < 1f)
                {
                    toRemove = item;
                }
            }

            toRemove?.DestroySelf();
            _items.Remove(toRemove);
        }

        public void Clear()
        {
            foreach (TileChunk chunk in _chunks.Values)
            {
                chunk.Clear();
            }

            _chunks.Clear();

            foreach (PlacedItemObject item in _items)
            {
                item.DestroySelf();
            }

            _items.Clear();
        }

        /// <summary>
        /// Returns a new SaveObject for storing the entire map.
        /// </summary>
        /// <returns></returns>
        public SavedTileMap Save()
        {
            List<SavedTileChunk> chunkObjectSaveList = new List<SavedTileChunk>();
            List<SavedPlacedItemObject> itemSaveList = new List<SavedPlacedItemObject>();

            foreach (TileChunk chunk in _chunks.Values)
            {
                chunkObjectSaveList.Add(chunk.Save());
            }

            foreach (PlacedItemObject item in _items)
            {
                itemSaveList.Add(item.Save());
            }

            return new SavedTileMap
            {
                mapName = _mapName,
                savedChunkList = chunkObjectSaveList.ToArray(),
                savedItemList = itemSaveList.ToArray()
            };
        }

        public void Load([CanBeNull] SavedTileMap saveObject)
        {
	        if (saveObject == null)
	        {       
                Log.Warning(this, "The intended save object is null");
		        return;
	        }

            Clear();

            TileSystem tileSystem = Subsystems.Get<TileSystem>();

            foreach (SavedTileChunk savedChunk in saveObject.savedChunkList)
            {
                TileChunk chunk = GetOrCreateChunk(savedChunk.originPosition);

                foreach (SavedTileObject savedTile in savedChunk.tileObjectSaveObjectArray)
                {
                    TileObjectSo toBePlaced = (TileObjectSo)tileSystem.GetAsset(savedTile.placedSaveObject.tileObjectSOName);
                    Vector3 placePosition = chunk.GetWorldPosition(savedTile.x, savedTile.y);

                    // Skipping build check here to allow loading tile objects in a non-valid order
                    PlaceTileObject(toBePlaced, placePosition, savedTile.placedSaveObject.dir, true, false);
                }
            }

            foreach (SavedPlacedItemObject savedItem in saveObject.savedItemList)
            {
                ItemObjectSo toBePlaced = (ItemObjectSo)tileSystem.GetAsset(savedItem.itemName);
                PlaceItemObject(savedItem.worldPosition, savedItem.rotation, toBePlaced);
            }
        }
    }
}