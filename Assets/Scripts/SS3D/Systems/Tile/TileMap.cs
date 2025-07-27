using FishNet;
using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Inventory.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Class used for storing and modifying a tile map. Coordinates on the tile map follows the following :
    /// - South North  is the Y axis with North going toward positives.
    /// - East West is the X axis with east going toward positives.
    /// </summary>
    public class TileMap : NetworkBehaviour
    {
        private Dictionary<Vector2Int, TileChunk> _chunks;
        private List<PlacedItemObject> _items;
        private string _mapName;

        public int ChunkCount => _chunks.Count;

        public event EventHandler OnMapLoaded;

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

        public TileChunk GetChunk(Vector3 worldPosition)
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

        public ITileLocation GetTileLocation(TileLayer layer, Vector3 worldPosition)
        {
            TileChunk chunk = GetOrCreateChunk(worldPosition); // TODO: creates unnessary empty chunk when checking whether building can be done
            return chunk.GetTileObject(layer, worldPosition);
        }


        public ITileLocation[] GetTileLocations(Vector3 worldPosition)
        {
            ITileLocation[] tileObjects = new ITileLocation[TileHelper.GetTileLayers().Length];

            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                tileObjects[(int)layer] = GetTileLocation(layer, worldPosition);
            }

            return tileObjects;
        }

        /// <summary>
        /// Returns an array of the 8 neighbouring placed objects of a given tile location.
        /// It should be noted that if the tile location is not a single object tile location (e.g a cardinal tile location),
        /// then it checks if on the tile next to it, an item is present in the opposite direction.
        /// Therefore it doesn't consider neighbours two objects on adjacent tiles, unless they're placed in opposite
        /// direction. For single object tile location, direction does not matter of course.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public PlacedTileObject[] GetNeighbourPlacedObjects(TileLayer layer, Vector3 worldPosition)
        {
            PlacedTileObject[] adjacentObjects = new PlacedTileObject[8];

            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++)
            {
                Tuple<int, int> vector = TileHelper.ToCardinalVector(direction);
                ITileLocation neighbourLocation = GetTileLocation(layer, worldPosition + new Vector3(vector.Item1, 0, vector.Item2));
                neighbourLocation.TryGetPlacedObject(out PlacedTileObject neighbourObject, TileHelper.GetOpposite(direction));
                adjacentObjects[(int)direction] = neighbourObject;
            }
            return adjacentObjects;
        }


        public List<PlacedTileObject> GetCardinalNeighbourPlacedObjects(TileLayer layer, Vector3 worldPosition)
        {
            List<PlacedTileObject> adjacentObjects = new();

            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction+= 2)
            {
                Tuple<int, int> vector = TileHelper.ToCardinalVector(direction);
                adjacentObjects.AddRange(
                    GetTileLocation(layer, worldPosition + new Vector3(vector.Item1, 0, vector.Item2)).GetAllPlacedObject());
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

                canBuild &= BuildChecker.CanBuild(GetTileLocations(gridPosition), tileObjectSo, dir, gridPosition,
                    GetNeighbourPlacedObjects(TileLayer.Turf, gridPosition), replaceExisting);
            }
            
            return canBuild;
        }

        public bool PlaceTileObject(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir,
            bool skipBuildCheck, bool replaceExisting, bool skipAdjacency, out GameObject placedObjectGo)
        {
            bool canBuild = CanBuild(tileObjectSo, placePosition, dir, replaceExisting);
            placedObjectGo = null;

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
                        ClearTileObject(gridPosition, tileObjectSo.layer, dir);

                    // Place new object
                    chunk.GetTileObject(tileObjectSo.layer, gridPosition).AddPlacedObject(placedObject, dir);
                }

                // Handle Adjacency connectors, can skip it particulary when loading the map.
                if (!skipAdjacency){
                    placedObject.UpdateAdjacencies();
                }

                placedObjectGo = placedObject.gameObject;
            }

            return canBuild;
        }

        public void ClearTileObject(Vector3 placePosition, TileLayer layer, Direction dir)
        {
            ITileLocation[] tileLocations = GetTileLocations(placePosition);
            ITileLocation tileLocation = tileLocations[(int)layer];
            tileLocation.TryGetPlacedObject(out var placed, dir);


            if (placed != null && placed.TryGetComponent(out IAdjacencyConnector connector))
            {
                List<PlacedTileObject> neighbours = connector.GetNeighbours();
                ResetAdjacencies(placed, tileLocation, neighbours);
            }
            else
            {
                tileLocation.TryClearPlacedObject(dir);
            }

            // Remove any invalid tile combinations
            List<ITileLocation> toClearLocations = BuildChecker.GetToBeClearedLocations(tileLocations);

            foreach (ITileLocation clearLocation in toClearLocations)
            {
                var allPlaced = clearLocation.GetAllPlacedObject();

                foreach (PlacedTileObject placedToClear in allPlaced)
                {
                    if (placed != null && placedToClear.TryGetComponent(out connector))
                    {
                        List<PlacedTileObject> neighbours = connector.GetNeighbours();
                        ResetAdjacencies(placed, clearLocation, neighbours);
                    }
                }

                clearLocation.ClearAllPlacedObject();
            }
        }

        private void ResetAdjacencies(PlacedTileObject placed, ITileLocation location, List<PlacedTileObject> neighbours)
        {
            List<Direction> neighboursAtDirection= new List<Direction>();

            // First get the directions of all neighbours, relative to this placed object.
            // Direction is not always relevant (e.g disposal pipes with disposal furnitures)
            // but it is in most cases. It's up to the connectors to choose what they do with this info.
            foreach (PlacedTileObject neighbour in neighbours)
            {
               placed.NeighbourAtDirectionOf(neighbour, out var dir);
               neighboursAtDirection.Add(dir);
            }
            // then destroy this placed object. It's important to do it here, before updating
            // adjacencies, as some connectors might be looking for it.
            location.ClearAllPlacedObject();

            // Then update all neighbours, using their directions.
            int i = 0;
            foreach (PlacedTileObject neighbour in neighbours)
            {
                Direction dir = neighboursAtDirection[i];
                neighbour?.UpdateSingleAdjacency(TileHelper.GetOpposite(dir), null, false);
                i++;
            }
        }

        public void PlaceItemObject(Vector3 worldPosition, Quaternion rotation, ItemObjectSo itemObjectSo, GameObject existingItem = null)
        {
            // Handle existing items that already have a PlacedItemObject component
            if (existingItem != null)
            {
                PlacedItemObject existingPlacedItem = existingItem.GetComponent<PlacedItemObject>();
                if (existingPlacedItem != null)
                {
                    if (_items.Contains(existingPlacedItem))
                    {
                        // Item is already tracked, just update its position
                        existingPlacedItem.UpdatePosition(worldPosition, rotation);
                        return;
                    }
                    else
                    {
                        // Item has PlacedItemObject but not tracked, remove the old component
                        DestroyImmediate(existingPlacedItem);
                    }
                }
            }
            
            // Create new PlacedItemObject and add to tracking
            PlacedItemObject placedItem = PlacedItemObject.Create(worldPosition, rotation, itemObjectSo, existingItem);
            placedItem.transform.SetParent(transform);
            _items.Add(placedItem);
        }

        public void ClearItemObject(Vector3 worldPosition, ItemObjectSo itemObjectSo)
        {
            List<PlacedItemObject> placedItems = _items.FindAll(item => item.NameString == itemObjectSo.NameString);
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

        /// <summary>
        /// Remove a specific PlacedItemObject from TileMap tracking without destroying the GameObject.
        /// This is used when items are picked up and placed in containers.
        /// </summary>
        /// <param name="placedItemObject">The PlacedItemObject to remove from tracking</param>
        public void RemovePlacedItemFromTracking(PlacedItemObject placedItemObject)
        {
            if (placedItemObject != null && _items.Contains(placedItemObject))
            {
                _items.Remove(placedItemObject);
            }
        }

        public void Clear()
        {
            foreach (TileChunk chunk in _chunks.Values)
            {
                chunk.Clear();
            }

            _chunks.Clear();

            // Clear items list safely, checking for null references
            while (_items.Count > 0)
            {
                PlacedItemObject item = _items.First();
                if (item != null && item.gameObject != null)
                {
                    item.DestroySelf();
                }
                
                _items.RemoveAt(0);
            }
        }

        /// <summary>
        /// Returns a new SaveObject for storing the entire map.
        /// Note: Items in player inventory (containers) are not saved as they have their PlacedItemObject
        /// component removed when picked up. Only items placed in the world are saved.
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

            // Clear TileMap data first (this clears the _items list)
            Clear();
            
            // Then clear all items in the scene, not just those tracked by TileMap
            ClearUntrackedItems();

            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();

            foreach (SavedTileChunk savedChunk in saveObject.savedChunkList)
            {
                TileChunk chunk = GetOrCreateChunk(savedChunk.originPosition);
                ISavedTileLocation[] savedTiles = savedChunk.savedTiles;

                foreach (var savedTile in savedTiles)
                {
                    foreach (SavedPlacedTileObject savedObject in savedTile.GetPlacedObjects())
                    {
                        TileObjectSo toBePlaced = (TileObjectSo)tileSystem.GetAsset(savedObject.tileObjectSOName);
                        Vector3 placePosition = chunk.GetWorldPosition(savedTile.Location.x, savedTile.Location.y);

                        // Skipping build check here to allow loading tile objects in a non-valid order
                        PlaceTileObject(toBePlaced, placePosition, savedObject.dir, true, false, true, out GameObject placedObject);
                    }
                }
            }

            foreach (SavedPlacedItemObject savedItem in saveObject.savedItemList)
            {
                ItemObjectSo toBePlaced = (ItemObjectSo)tileSystem.GetAsset(savedItem.itemName);
                PlaceItemObject(savedItem.worldPosition, savedItem.rotation, toBePlaced);
            }

            OnMapLoaded?.Invoke(this, EventArgs.Empty);
            UpdateAllAdjacencies();
        }

        /// <summary>
        /// Update every adjacency of each placed tile object when the map is loaded.
        /// </summary>
        private void UpdateAllAdjacencies()
        {
            foreach(TileChunk chunk in _chunks.Values)
            {
                foreach(PlacedTileObject obj in chunk.GetAllTilePlacedObjects())
                {
                    if (obj.HasAdjacencyConnector)
                    {
                        var pos = chunk.GetWorldPosition(obj.Origin.x, obj.Origin.y);
                        obj.UpdateAdjacencies();
                    }
                }
            }
        }

        /// <summary>
        /// Clear untracked items in the scene that are not in containers and don't have a PlacedItemObject component
        /// </summary>
        private void ClearUntrackedItems()
        {
            // Find all Item components in the scene
            Item[] allItems = FindObjectsOfType<Item>();
            
            foreach (Item item in allItems)
            {
                // Skip items that are in containers (player inventory, etc.)
                // and items that already have PlacedItemObject (they're already tracked)
                if (item.Container != null || item.GetComponent<PlacedItemObject>() != null)
                {
                    continue;
                }
                
                // Destroy items that are in the world but not tracked by TileMap
                if (item.gameObject != null)
                {
                    Debug.LogWarning($"Destroying untracked item: {item.gameObject.name} at position {item.gameObject.transform.position}");
                    DestroyImmediate(item.gameObject);
                }
            }
        }
    }
}