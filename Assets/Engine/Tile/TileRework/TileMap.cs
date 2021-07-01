using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.TilesRework
{
    public class TileMap : MonoBehaviour
    {
        [Serializable]
        public class SaveObject
        {
            public string mapName;
            public TileChunk.SaveObject[] saveObjectList;
        }

        private const int CHUNK_SIZE = 16;
        private const float TILE_SIZE = 1.0f;

        private Dictionary<ulong, TileChunk> chunks;
        private string mapName;
        private TileManager tileManager;

        void Awake()
        {
            chunks = new Dictionary<ulong, TileChunk>();
        }

        public static TileMap Create(string name)
        {
            GameObject mapObject = new GameObject(name);

            TileMap map = mapObject.AddComponent<TileMap>();
            map.Setup(name);

            return map;
        }

        private void Setup(string name)
        {
            tileManager = TileManager.Instance;
            this.name = name;
        }

        public string GetName()
        {
            return mapName;
        }

        private TileChunk CreateChunk(ulong chunkKey, Vector3 origin)
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
        }

        private ulong GetKey(int chunkX, int chunkY)
        {
            return ((ulong)chunkX << 32) + (ulong)chunkY;
        }

        private ulong GetKey(Vector3 worldPosition)
        {
            int x = (int)Math.Floor(worldPosition.x / CHUNK_SIZE);
            int y = (int)Math.Floor(worldPosition.z / CHUNK_SIZE);

            return (GetKey(x, y));
        }

        private TileChunk GetChunk(Vector3 worldPosition)
        {
            ulong key = GetKey(worldPosition);

            // Create a new chunk if there is none
            if (chunks[key] == null)
            {
                int x = (int)(key & 0xffff);
                int y = (int)(key >> 32);

                Vector3 origin = new Vector3 {x = x, z = y};
                chunks[key] = CreateChunk(key, origin);
            }

            return chunks[key];
        }

        public void SetTileObject(TileLayerType layer, TileObjectSO tileObjectSO, Vector3 position, Direction dir)
        {
            // Get the right chunk
            TileChunk chunk = GetChunk(position);
            Vector2Int vector = chunk.GetXY(position);

            Vector2Int placedObjectOrigin = new Vector2Int(vector.x, vector.y);
            placedObjectOrigin = chunk.ValidateGridPosition(placedObjectOrigin);

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
            vector = chunk.ValidateGridPosition(vector);
            PlacedTileObject placedObject = chunk.GetTileObject(layer, vector.x, vector.y).GetPlacedObject();

            List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                chunk.GetTileObject(layer, gridPosition.x, gridPosition.y).ClearPlacedObject();
            }
        }

        public SaveObject Save()
        {
            List<TileChunk.SaveObject> chunkObjectSaveList = new List<TileChunk.SaveObject>();

            foreach (TileChunk chunk in chunks.Values)
            {
                chunkObjectSaveList.Add(chunk.Save());
            }

            return new SaveObject
            {
                mapName = mapName,
                saveObjectList = chunkObjectSaveList.ToArray(),
            };
        }

        
        public void Load(SaveObject saveObject)
        {
            /*
            foreach (TileChunk.SaveObject tileObjectSaveObject in saveObject.saveObjectList)
            {
                TileLayerType layer = tileObjectSaveObject.layer;
                string objectName = tileObjectSaveObject.placedSaveObject.tileObjectSOName;

                SetTileObject(layer, objectName, GetWorldPosition(tileObjectSaveObject.x, tileObjectSaveObject.y), tileObjectSaveObject.placedSaveObject.dir);
            }
            */
        }
        
    }
}