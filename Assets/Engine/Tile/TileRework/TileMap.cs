using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Tiles
{
    public class TileMap : MonoBehaviour
    {
        [Serializable]
        public class MapSaveObject
        {
            public string mapName;
            public bool isMain;
            public TileChunk.ChunkSaveObject[] saveObjectList;
        }

        public const int CHUNK_SIZE = 16;
        private const float TILE_SIZE = 1.0f;

        private Dictionary<Vector2Int, TileChunk> chunks;
        public int ChunkCount { get => chunks.Count; }
        public bool IsMain { get; set; }

        private string mapName;
        private TileManager tileManager;

        public static TileMap Create(string name)
        {
            GameObject mapObject = new GameObject(name);

            TileMap map = mapObject.AddComponent<TileMap>();
            map.Setup(name);

            return map;
        }

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

        public void SetEnabled(TileLayer layer, bool enabled)
        {
            foreach (TileChunk chunk in chunks.Values)
            {
                chunk.SetEnabled(layer, enabled);
            }
        }

        private TileChunk CreateChunk(Vector2Int chunkKey, Vector3 origin)
        {
            TileChunk chunk = new TileChunk(chunkKey, CHUNK_SIZE, CHUNK_SIZE, TILE_SIZE, origin);
            return chunk;
        }

        public void Clear()
        {
            foreach (TileChunk chunk in chunks.Values)
            {
                chunk.Clear();
            }

            chunks.Clear();
        }

        private Vector2Int GetKey(int chunkX, int chunkY)
        {
            return new Vector2Int(chunkX, chunkY);
        }

        private Vector2Int GetKey(Vector3 worldPosition)
        {
            int x = (int)Math.Floor(worldPosition.x / CHUNK_SIZE);
            int y = (int)Math.Floor(worldPosition.z / CHUNK_SIZE);

            return (GetKey(x, y));
        }

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

        private void DeleteIfEmpty(TileChunk chunk)
        {
            if (chunk.IsEmpty())
                chunks.Remove(GetKey(chunk.GetOrigin()));
        }

        public bool CanBuild(int subLayerIndex, TileObjectSO tileObjectSO, Vector3 position, Direction dir)
        {
            // Get the right chunk
            TileChunk chunk = GetOrCreateChunk(position);
            Vector2Int vector = chunk.GetXY(position);
            Vector2Int placedObjectOrigin = new Vector2Int(vector.x, vector.y);
            TileLayer layer = tileObjectSO.layerType;

            // Test Can Build
            List<Vector2Int> gridPositionList = tileObjectSO.GetGridPositionList(placedObjectOrigin, dir);

            bool canBuild = true;

            canBuild = TileRestrictions.CanBuild(this, position, subLayerIndex, tileObjectSO, dir);

            foreach (Vector2Int gridPosition in gridPositionList)
            {
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

        public void SetTileObject(int subLayerIndex, TileObjectSO tileObjectSO, Vector3 position, Direction dir)
        {
            TileLayer layer = tileObjectSO.layerType;
            GameObject layerObject = GetOrCreateLayerObject(layer);

            // Get the right chunk
            TileChunk chunk = GetOrCreateChunk(position);
            Vector2Int vector = chunk.GetXY(position);
            Vector2Int placedObjectOrigin = new Vector2Int(vector.x, vector.y);

            // Test Can Build
            List<Vector2Int> gridPositionList = tileObjectSO.GetGridPositionList(placedObjectOrigin, dir);

            if (CanBuild(subLayerIndex, tileObjectSO, position, dir))
            {
                // Get the chunk again as it may be deleted in CanBuild()
                chunk = GetOrCreateChunk(position);

                Vector2Int rotationOffset = tileObjectSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = chunk.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * chunk.GetTileSize();

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

        public void LoadTileObject(int subLayerIndex, TileObjectSO tileObjectSO, PlacedTileObject placedObject, Vector3 position, Direction dir)
        {
            TileLayer layer = tileObjectSO.layerType;
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
                        nextChunk.GetTileObject(layer, offEdgeObjectPosition).SetPlacedObject(placedObject, subLayerIndex);
                    }
                    else
                    {
                        chunk.GetTileObject(layer, gridPosition.x, gridPosition.y).ClearPlacedObject(subLayerIndex);
                        UpdateAdjacencies(layer, position);
                    }
                }
            }

            DeleteIfEmpty(chunk);
        }

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

        public void UpdateAllAdjacencies()
        {
            var adjacentObjects = new PlacedTileObject[8];

            // Loop through every single tile object...
            foreach (TileChunk chunk in chunks.Values)
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
                                TileObjectSO tileObjectSO = tileManager.GetTileObjectSO(objectName);

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

        private GameObject FindChild(TileLayer layer, int subLayerIndex, Vector3 position)
        {
            Transform layerObjectTransform = GetOrCreateLayerObject(layer).transform;

            // For walls, multiple object can exist at the same location. 
            // So use the naming convention to determine at which rotation it is placed
            bool sameTile = (layer == TileLayer.LowWall || layer == TileLayer.HighWall);

            for (int i = 0; i < layerObjectTransform.childCount; i++)
            {
                var child = layerObjectTransform.GetChild(i);
                if (child.position == position)
                {
                    if (sameTile && !child.name.Contains("_" + subLayerIndex))
                        continue;

                    return child.gameObject;
                }
                    
            }

            return null;
        }

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