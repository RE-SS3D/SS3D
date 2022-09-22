using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Engine.Tile.TileRework;
using UnityEditor;
using UnityEngine;
using static SS3D.Engine.Tile.TileRework.TileRestrictions;

namespace SS3D.Engine.Tiles
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
            public bool isMain;
            public TileChunk.ChunkSaveObject[] saveObjectList;
        }

        /// <summary>
        /// Number of TileObjects that should go in a chunk. 16 x 16
        /// </summary>
        public const int CHUNK_SIZE = 16;

        /// <summary>
        /// The size of each tile.
        /// </summary>
        private const float TILE_SIZE = 1.0f;

        /// <summary>
        /// The tolerance threshold for identifying a tile object at a position.
        /// </summary>
        private const float POSITION_TOLERANCE = 0.05f;

        private Dictionary<Vector2Int, TileChunk> chunks;
        public int ChunkCount { get => chunks.Count; }
        public bool IsMain { get; set; }

        private string mapName;
        private TileManager tileManager;

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
        /// <param name="name"></param>
        public void Setup(string name)
        {
            chunks = new Dictionary<Vector2Int, TileChunk>();
            tileManager = TileManager.Instance;
            this.name = name;
            mapName = name;
        }

        public string GetName()
        {
            return mapName;
        }

        public void SetName(string name)
        {
            this.mapName = name;
            gameObject.name = name;
        }

        /// <summary>
        /// Set all objects either enabled or disabled for a layer.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="enabled"></param>
        public void SetEnabled(TileLayer layer, bool enabled)
        {
            foreach (TileChunk chunk in chunks.Values)
            {
                chunk.SetEnabled(layer, enabled);
            }
        }

        /// <summary>
        /// Get a chunk for the passed chunk key
        /// </summary>
        /// <param name="chunkKey">The key for the chunk</param>
        public TileChunk GetChunk(Vector2Int chunkKey)
        {
            chunks.TryGetValue(chunkKey, out TileChunk value);
            return value;
        }

        /// <summary>
        /// Create a new chunk.
        /// </summary>
        /// <param name="chunkKey">Unique key to use</param>
        /// <param name="origin">Origin position of the chunk</param>
        /// <returns></returns>
        private TileChunk CreateChunk(Vector2Int chunkKey, Vector3 origin)
        {
            TileChunk chunk = new TileChunk(chunkKey, CHUNK_SIZE, CHUNK_SIZE, TILE_SIZE, origin);
            return chunk;
        }

        /// <summary>
        /// Clears the entire map.
        /// </summary>
        public void Clear()
        {
            // Number of chunks can be modified during deletion, so create a copy
            List<TileChunk> tempChunkList = new List<TileChunk>();
            tempChunkList.AddRange(chunks.Values);

            foreach (TileChunk chunk in tempChunkList)
            {
                chunk.Clear();
            }

            chunks.Clear();
        }

        /// <summary>
        /// Returns the chunk key to used based on an X and Y offset.
        /// </summary>
        /// <param name="chunkX"></param>
        /// <param name="chunkY"></param>
        /// <returns></returns>
        private Vector2Int GetKey(int chunkX, int chunkY)
        {
            return new Vector2Int(chunkX, chunkY);
        }

        /// <summary>
        /// Returns the chunk key to be used based on a world position.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public Vector2Int GetKey(Vector3 worldPosition)
        {
            int x = (int)Math.Floor(worldPosition.x / CHUNK_SIZE);
            int y = (int)Math.Floor(worldPosition.z / CHUNK_SIZE);

            return (GetKey(x, y));
        }

        /// <summary>
        /// Returns chunk based the world position. Will create a new chunk if it doesn't exist.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        private TileChunk GetOrCreateChunk(Vector3 worldPosition)
        {
            Vector2Int key = GetKey(worldPosition);
            TileChunk chunk;

            // Create a new chunk if there is none
            if (!chunks.TryGetValue(key, out chunk))
            {
                Vector3 origin = new Vector3 {x = key.x * CHUNK_SIZE, z = key.y * CHUNK_SIZE };
                chunks[key] = CreateChunk(key, origin);
            }

            return chunks[key];
        }

        public TileChunk[] GetChunks()
        {
            return chunks?.Values.ToArray();
        }

        /// <summary>
        /// Deletes a chunk if all TileObjects do not have a PlacedTileObject.
        /// </summary>
        /// <param name="chunk"></param>
        private void DeleteIfEmpty(TileChunk chunk)
        {
            if (chunk.IsEmpty())
                chunks.Remove(GetKey(chunk.GetOrigin()));
        }

        /// <summary>
        /// Returns whether the specified object can be successfully build for a given position and direction.
        /// </summary>
        /// <param name="subLayerIndex">Sub layer the object should go. Usually zero</param>
        /// <param name="tileObjectSO">Object to place</param>
        /// <param name="position">World position to place the object</param>
        /// <param name="dir">Direction the object is facing</param>
        /// <returns></returns>
        public bool CanBuild(int subLayerIndex, TileObjectSo tileObjectSO, Vector3 position, Direction dir, CheckRestrictions checkRestrictions)
        {
            // Get the right chunk
            TileChunk chunk = GetOrCreateChunk(position);
            Vector2Int vector = chunk.GetXY(position);
            Vector2Int placedObjectOrigin = new Vector2Int(vector.x, vector.y);
            TileLayer layer = tileObjectSO.layer;

            List<Vector2Int> gridPositionList = tileObjectSO.GetGridPositionList(placedObjectOrigin, dir);

            bool canBuild = true;
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                // Verify if we are allowed to build for this grid position
                Vector3 checkWorldPosition = chunk.GetWorldPosition(gridPosition.x, gridPosition.y);

                if (checkRestrictions == CheckRestrictions.Everything)
                    canBuild &= TileRestrictions.CanBuild(this, checkWorldPosition, subLayerIndex, tileObjectSO, dir);
                else if (checkRestrictions == CheckRestrictions.OnlyRestrictions)
                {
                    canBuild &= TileRestrictions.CanBuild(this, checkWorldPosition, subLayerIndex, tileObjectSO, dir);
                    continue;
                }

                if (chunk.GetTileObject(layer, gridPosition.x, gridPosition.y) == null)
                {
                    // We got a chunk edge case in which a multi tile object is outside of the chunk
                    Vector3 offEdgeObjectPosition = chunk.GetWorldPosition(gridPosition.x, gridPosition.y);
                    TileChunk nextChunk = GetOrCreateChunk(offEdgeObjectPosition);
                    if (!nextChunk.GetTileObject(layer, offEdgeObjectPosition).IsEmpty(subLayerIndex))
                    {
                        canBuild = false;
                        break;
                    }
                    DeleteIfEmpty(nextChunk);
                }
                else
                {
                    if (!chunk.GetTileObject(layer, gridPosition.x, gridPosition.y).IsEmpty(subLayerIndex))
                    {
                        canBuild = false;
                        break;
                    }
                }
            }

            DeleteIfEmpty(chunk);

            return canBuild;
        }

        /// <summary>
        /// Sets a specified TileObject on the map.
        /// </summary>
        /// <param name="subLayerIndex">Sub layer the object should go. Usually zero</param>
        /// <param name="tileObjectSO">Object to place</param>
        /// <param name="position">World position to place the object</param>
        /// <param name="dir">Direction the object is facing</param
        public void SetTileObject(int subLayerIndex, TileObjectSo tileObjectSO, Vector3 position, Direction dir)
        {
            TileLayer layer = tileObjectSO.layer;
            GameObject layerObject = GetOrCreateLayerObject(layer);

            // Get the right chunk
            TileChunk chunk = GetOrCreateChunk(position);
            Vector2Int vector = chunk.GetXY(position);
            Vector2Int placedObjectOrigin = new Vector2Int(vector.x, vector.y);

            // Test Can Build
            List<Vector2Int> gridPositionList = tileObjectSO.GetGridPositionList(placedObjectOrigin, dir);

            if (CanBuild(subLayerIndex, tileObjectSO, position, dir, CheckRestrictions.Everything))
            {
                // Get the chunk again as it may be deleted in CanBuild()
                chunk = GetOrCreateChunk(position);

                Vector2Int rotationOffset = tileObjectSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = chunk.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) + 
                    tileObjectSO.prefab.transform.position + new Vector3(rotationOffset.x, 0, rotationOffset.y) * chunk.GetTileSize();

                PlacedTileObject placedObject = PlacedTileObject.Create(placedObjectWorldPosition, placedObjectOrigin, dir, tileObjectSO);
                placedObject.transform.SetParent(layerObject.transform);

                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    if (chunk.GetTileObject(layer, gridPosition.x, gridPosition.y) == null)
                    {
                        // We got a chunk edge case in which a multi tile object is outside of the chunk
                        Vector3 offEdgeObjectPosition = chunk.GetWorldPosition(gridPosition.x, gridPosition.y);
                        TileChunk nextChunk = GetOrCreateChunk(offEdgeObjectPosition);
                        nextChunk.GetTileObject(layer, offEdgeObjectPosition).SetPlacedObject(placedObject, subLayerIndex);
                    }
                    else
                    {
                        chunk.GetTileObject(layer, gridPosition.x, gridPosition.y).SetPlacedObject(placedObject, subLayerIndex);
                    }
                    UpdateAdjacencies(layer, position);
                }
            }
            else
            {
                Debug.LogWarning("Cannot build here");
            }
        }

        /// <summary>
        /// Sets a PlacedTileObject on the map based on a known object, position and direction. Used for reconstructing the map structure from a save file.
        /// </summary>
        /// <param name="subLayerIndex"></param>
        /// <param name="tileObjectSO"></param>
        /// <param name="placedObject"></param>
        /// <param name="position"></param>
        /// <param name="dir"></param>
        public void LoadTileObject(int subLayerIndex, TileObjectSo tileObjectSO, PlacedTileObject placedObject, Vector3 position, Direction dir)
        {
            TileLayer layer = tileObjectSO.layer;
            GameObject layerObject = GetOrCreateLayerObject(layer);

            // Get the right chunk
            TileChunk chunk = GetOrCreateChunk(position);
            Vector2Int vector = chunk.GetXY(position);
            Vector2Int placedObjectOrigin = new Vector2Int(vector.x, vector.y);

            List<Vector2Int> gridPositionList = tileObjectSO.GetGridPositionList(placedObjectOrigin, dir);

            Vector2Int rotationOffset = tileObjectSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = chunk.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * chunk.GetTileSize();

            placedObject.Setup(tileObjectSO, placedObjectOrigin, dir);
            placedObject.transform.SetParent(layerObject.transform);

            foreach (Vector2Int gridPosition in gridPositionList)
            {
                if (chunk.GetTileObject(layer, gridPosition.x, gridPosition.y) == null)
                {
                    // We got a chunk edge case in which a multi tile object is outside of the chunk
                    Vector3 offEdgeObjectPosition = chunk.GetWorldPosition(gridPosition.x, gridPosition.y);
                    TileChunk nextChunk = GetOrCreateChunk(offEdgeObjectPosition);
                    nextChunk.GetTileObject(layer, offEdgeObjectPosition).SetPlacedObject(placedObject, subLayerIndex);
                }
                else
                {
                    chunk.GetTileObject(layer, gridPosition.x, gridPosition.y).SetPlacedObject(placedObject, subLayerIndex);
                }
            }
        }

        /// <summary>
        /// Clear a PlacedTileObject at a given world position and layer.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="subLayerIndex"></param>
        /// <param name="position"></param>
        public void ClearTileObject(TileLayer layer, int subLayerIndex, Vector3 position)
        {
            TileChunk chunk = GetOrCreateChunk(position);

            Vector2Int vector = chunk.GetXY(position);
            PlacedTileObject placedObject = chunk.GetTileObject(layer, vector.x, vector.y).GetPlacedObject(subLayerIndex);

            if (placedObject != null)
            {
                // Destroy any objects that are on top
                foreach (var topPlacedObject in TileRestrictions.GetToBeDestroyedObjects(this, layer, position))
                {
                    topPlacedObject.ClearAllPlacedObjects();
                }

                List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    if (chunk.GetTileObject(layer, gridPosition.x, gridPosition.y) == null)
                    {
                        // We got a chunk edge case in which a multi tile object is outside of the chunk
                        Vector3 offEdgeObjectPosition = chunk.GetWorldPosition(gridPosition.x, gridPosition.y);
                        TileChunk nextChunk = GetOrCreateChunk(offEdgeObjectPosition);
                        nextChunk.GetTileObject(layer, offEdgeObjectPosition).ClearPlacedObject(subLayerIndex);
                    }
                    else
                    {
                        chunk.GetTileObject(layer, gridPosition.x, gridPosition.y).ClearPlacedObject(subLayerIndex);
                    }

                    UpdateAdjacencies(layer, position);
                }
            }

            DeleteIfEmpty(chunk);
        }

        /// <summary>
        /// Returns a TileObject at a given layer and world position.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public TileObject GetTileObject(TileLayer layer, Vector3 worldPosition)
        {
            TileChunk chunk = GetOrCreateChunk(worldPosition);
            return chunk.GetTileObject(layer, worldPosition);
        }

        public void UpdateAdjacencies(TileLayer layer, Vector3 worldPosition)
        {
            var adjacentObjects = new PlacedTileObject[8];
            TileObject currentTileObject = GetTileObject(layer, worldPosition);

            // Find the neighbours in each direction
            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++)
            {
                var vector = TileHelper.ToCardinalVector(direction);
                TileObject neighbour = GetTileObject(layer, worldPosition + new Vector3(vector.Item1, 0, vector.Item2));

                adjacentObjects[(int)direction] = neighbour.GetPlacedObject(0);
                neighbour.GetPlacedObject(0)?.UpdateSingleAdjacency(TileHelper.GetOpposite(direction), currentTileObject.GetPlacedObject(0));
            }

            currentTileObject.GetPlacedObject(0)?.UpdateAllAdjacencies(adjacentObjects);
        }

        /// <summary>
        /// Updates all objects that contain an AdjacencyConnector. Will loop through the entire map, so use this sparingly. 
        /// </summary>
        public void UpdateAllAdjacencies()
        {
            var adjacentObjects = new PlacedTileObject[8];

            // Chunks dictionary can be modified in the time between...
            List<TileChunk> tempChunkList = new List<TileChunk>();
            tempChunkList.AddRange(chunks.Values);

            // Loop through every single tile object...
            foreach (TileChunk chunk in tempChunkList)
            {
                foreach (TileLayer layer in TileHelper.GetTileLayers())
                {
                    for (int x = 0; x < chunk.GetWidth(); x++)
                    {
                        for (int y = 0; y < chunk.GetHeight(); y++)
                        {
                            // Make sure that there is an placed object and that we have an adjacency connector
                            TileObject tileObject = chunk.GetTileObject(layer, x, y);
                            if (!tileObject.IsEmpty(0) && tileObject.GetPlacedObject(0).HasAdjacencyConnector())
                            {
                                // Find the neighbours in each direction
                                Vector3 currentPosition = chunk.GetWorldPosition(x, y);
                                adjacentObjects = GetNeighbourObjects(layer, 0, currentPosition);
                                tileObject.GetPlacedObject(0).UpdateAllAdjacencies(adjacentObjects);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves neighbouring objects for a given position and layer.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="subLayerIndex"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public PlacedTileObject[] GetNeighbourObjects(TileLayer layer, int subLayerIndex, Vector3 position)
        {
            var adjacentObjects = new PlacedTileObject[8];

            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++)
            {
                var vector = TileHelper.ToCardinalVector(direction);
                TileObject neighbour = GetTileObject(layer, position + new Vector3(vector.Item1, 0, vector.Item2));

                adjacentObjects[(int)direction] = neighbour.GetPlacedObject(subLayerIndex);
            }

            return adjacentObjects;
        }

        /// <summary>
        /// Returns a new SaveObject for storing the entire map.
        /// </summary>
        /// <returns></returns>
        public MapSaveObject Save()
        {
            List<TileChunk.ChunkSaveObject> chunkObjectSaveList = new List<TileChunk.ChunkSaveObject>();

            foreach (TileChunk chunk in chunks.Values)
            {
                chunkObjectSaveList.Add(chunk.Save());
            }

            return new MapSaveObject
            {
                mapName = mapName,
                isMain = IsMain,
                saveObjectList = chunkObjectSaveList.ToArray(),
            };
        }
        
        /// <summary>
        /// Loads a given map from a save object. The softload parameter determines if saved objects should be
        /// new created, or only reinitalized.
        /// </summary>
        /// <param name="saveObject"></param>
        /// <param name="softLoad"></param>
        public void Load(MapSaveObject saveObject, bool softLoad)
        {
            if (!tileManager) tileManager = FindObjectOfType<TileManager>();

            IsMain = saveObject.isMain;

            // Loop through every chunk in map
            foreach (var chunk in saveObject.saveObjectList)
            {
                // Loop through every tile object in chunk
                foreach (var tileObjectSaveObject in chunk.tileObjectSaveObjectArray)
                {
                    TileLayer layer = tileObjectSaveObject.layer;
                    for (int subLayerIndex = 0; subLayerIndex < TileHelper.GetSubLayerSize(layer); subLayerIndex++)
                    {
                        string objectName = tileObjectSaveObject.placedSaveObjects[subLayerIndex].tileObjectSOName;
                        if (!objectName.Equals(""))
                        {
                            if (softLoad)
                            {
                                TileObjectSo tileObjectSO = tileManager.GetTileObjectSO(objectName);

                                // Find the object and set it up again...
                                Vector3 position = TileHelper.GetWorldPosition(tileObjectSaveObject.x, tileObjectSaveObject.y, chunk.tileSize, chunk.originPosition);
                                PlacedTileObject placedTileObject = FindChild(layer, subLayerIndex, position)?.GetComponent<PlacedTileObject>();

                                if (!placedTileObject)
                                {
                                    Debug.LogWarning("Child was not found when reinitializing: " + objectName);
                                    continue;
                                }

                                LoadTileObject(subLayerIndex, tileObjectSO, placedTileObject, position, tileObjectSaveObject.placedSaveObjects[subLayerIndex].dir);
                            }
                            else
                            {
                                tileManager.SetTileObject(this, subLayerIndex, objectName, TileHelper.GetWorldPosition(tileObjectSaveObject.x, tileObjectSaveObject.y, chunk.tileSize, chunk.originPosition)
                                    , tileObjectSaveObject.placedSaveObjects[subLayerIndex].dir);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds a PlacedTileObject for a given layer and position. Is used during softloading to retrieve the matching gameobject.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="subLayerIndex"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private GameObject FindChild(TileLayer layer, int subLayerIndex, Vector3 position)
        {
            Transform layerObjectTransform = GetOrCreateLayerObject(layer).transform;

            // For walls, multiple object can exist at the same location. 
            // So use the naming convention to determine at which rotation it is placed
            bool sameTile = TileHelper.ContainsSubLayers(layer);

            for (int i = 0; i < layerObjectTransform.childCount; i++)
            {
                var child = layerObjectTransform.GetChild(i);

                // There can be small offsets in height for some objects like overlays, so use only X and Z
                if ((Math.Abs(child.position.x - position.x) < POSITION_TOLERANCE) && (Math.Abs(child.position.z - position.z) < POSITION_TOLERANCE))
                {
                    if (sameTile && !child.name.Contains("_" + subLayerIndex))
                        continue;

                    return child.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Get or creates an empty gameobject that used to storing that layer's PlacedTileObjects.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private GameObject GetOrCreateLayerObject(TileLayer layer)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).name == layer.ToString())
                    return transform.GetChild(i).gameObject;
            }

            GameObject layerObject = new GameObject(layer.ToString());
            layerObject.transform.SetParent(transform);
            return layerObject;
        }
    }
}