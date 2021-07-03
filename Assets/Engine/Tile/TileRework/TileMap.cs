﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Engine.TilesRework
{
    public class TileMap : MonoBehaviour
    {
        [Serializable]
        public class MapSaveObject
        {
            public string mapName;
            public TileChunk.ChunkSaveObject[] saveObjectList;
        }

        public const int CHUNK_SIZE = 16;
        private const float TILE_SIZE = 1.0f;

        private Dictionary<Vector2Int, TileChunk> chunks;
        public int ChunkCount { get => chunks.Count; }

        private string mapName;
        private TileManager tileManager;

        public static TileMap Create(string name)
        {
            GameObject mapObject = new GameObject(name);

            TileMap map = mapObject.AddComponent<TileMap>();
            map.Setup(name);

            return map;
        }

        private void Setup(string name)
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

        public void SetEnabled(TileLayerType layer, bool enabled)
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

        private TileChunk GetChunk(Vector3 worldPosition)
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
            return chunks.Values.ToArray();
        }

        public void SetTileObject(TileLayerType layer, TileObjectSO tileObjectSO, Vector3 position, Direction dir)
        {
            // Get the right chunk
            TileChunk chunk = GetChunk(position);
            Vector2Int vector = chunk.GetXY(position);

            // Vector2Int placedObjectOrigin = new Vector2Int(vector.x, vector.y);
            // placedObjectOrigin = chunk.ValidateGridPosition(placedObjectOrigin);

            Vector2Int placedObjectOrigin = new Vector2Int(vector.x, vector.y);

            // Test Can Build
            List<Vector2Int> gridPositionList = tileObjectSO.GetGridPositionList(placedObjectOrigin, dir);
            bool canBuild = true;
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                if (!chunk.GetTileObject(layer, gridPosition.x, gridPosition.y).IsEmpty())
                {
                    canBuild = false;
                    break;
                }
            }

            if (canBuild)
            {
                Vector2Int rotationOffset = tileObjectSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = chunk.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * chunk.GetTileSize();

                PlacedTileObject placedObject = PlacedTileObject.Create(placedObjectWorldPosition, placedObjectOrigin, dir, tileObjectSO);
                placedObject.transform.SetParent(transform);

                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    chunk.GetTileObject(layer, gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
                }
            }
            else
            {
                Debug.LogWarning("Cannot build here");
            }
        }

        public void ClearTileObject(TileLayerType layer, Vector3 position)
        {
            TileChunk chunk = GetChunk(position);

            Vector2Int vector = chunk.GetXY(position);
            // vector = chunk.ValidateGridPosition(vector);
            PlacedTileObject placedObject = chunk.GetTileObject(layer, vector.x, vector.y).GetPlacedObject();

            if (placedObject != null)
            {
                List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    chunk.GetTileObject(layer, gridPosition.x, gridPosition.y).ClearPlacedObject();
                }
            }
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
                saveObjectList = chunkObjectSaveList.ToArray(),
            };
        }
        
        public void Load(MapSaveObject saveObject)
        {
            // Loop through every chunk in map
            foreach (var chunk in saveObject.saveObjectList)
            {
                // Loop through every tile object in chunk
                foreach (var tileObjectSaveObject in chunk.tileObjectSaveObjectArray)
                {
                    TileLayerType layer = tileObjectSaveObject.layer;
                    string objectName = tileObjectSaveObject.placedSaveObject.tileObjectSOName;

                    tileManager.SetTileObject(this, layer, objectName, TileHelper.GetWorldPosition(tileObjectSaveObject.x, tileObjectSaveObject.y, chunk.tileSize , chunk.originPosition)
                        , tileObjectSaveObject.placedSaveObject.dir);
                }
            }
        }
    }
}