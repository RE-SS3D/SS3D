using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.TilesRework
{
    public class TileChunk
    {
        public event EventHandler<OnGridObjectChangedEventArgs> OnGridObjectChanged;
        public class OnGridObjectChangedEventArgs : EventArgs
        {
            public int x;
            public int y;
        }

        public struct TileGrid
        {
            public TileLayer layer;
            public TileObject[] tileObjectsGrid;
        }

        [Serializable]
        public class ChunkSaveObject
        {
            public Vector2Int chunkKey;
            public int width;
            public int height;
            public float tileSize;
            public Vector3 originPosition;
            public TileObject.TileSaveObject[] tileObjectSaveObjectArray;
        }

        private Vector2Int chunkKey;
        private int width;
        private int height;
        private float tileSize = 1f;
        private Vector3 originPosition;
        private List<TileGrid> tileGridList;
        private TileManager tileManager;

        public TileChunk(Vector2Int chunkKey, int width, int height, float tileSize, Vector3 originPosition)
        {
            this.chunkKey = chunkKey;
            this.width = width;
            this.height = height;
            this.tileSize = tileSize;
            this.originPosition = originPosition;

            CreateAllGrids();
            tileManager = TileManager.Instance;
        }

        private TileGrid CreateGrid(TileLayer layer)
        {
            TileGrid grid = new TileGrid { layer = layer };

            int gridSize = width * height;
            int subLayerMultiplier = 1;

            switch (layer)
            {
                case TileLayer.Pipes:
                case TileLayer.Overlays:
                    subLayerMultiplier = 3;
                    break;
                case TileLayer.HighWall:
                case TileLayer.LowWall:
                    subLayerMultiplier = 4;
                    break;
            }

            grid.tileObjectsGrid = new TileObject[gridSize * subLayerMultiplier];
            for (int i = 0; i < subLayerMultiplier; i++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        grid.tileObjectsGrid[y * width + x + (gridSize * i)] = new TileObject(this, layer, x, y);
                    }
                }
            }

            return grid;
        }

        private void CreateAllGrids()
        {
            tileGridList = new List<TileGrid>();

            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                tileGridList.Add(CreateGrid(layer));
            }
        }

        public int GetWidth()
        {
            return width;
        }

        public int GetHeight()
        {
            return height;
        }

        public float GetTileSize()
        {
            return tileSize;
        }

        public Vector3 GetOrigin()
        {
            return originPosition;
        }

        public Vector2Int GetKey()
        {
            return chunkKey;
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x, 0, y) * tileSize + originPosition;
        }

        public Vector2Int GetXY(Vector3 worldPosition)
        {
            return new Vector2Int((int)Math.Round(worldPosition.x - originPosition.x), (int)Math.Round(worldPosition.z - originPosition.z));
        }

        public void SetEnabled(TileLayer layer, bool enabled)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                   GetTileObject(layer, x, y).GetPlacedObject()?.gameObject.SetActive(enabled);
                }
            }
        }

        public void SetTileObject(TileLayer layer, int subLayerIndex, int x, int y, TileObject value)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                int subLayerOffset = width * height * subLayerIndex;
                tileGridList[(int)layer].tileObjectsGrid[y * width + x + subLayerOffset] = value;
                TriggerGridObjectChanged(x, y);
            }
        }

        public void SetTileObject(TileLayer layer, int subLayerIndex, Vector3 worldPosition, TileObject value)
        {
            Vector2Int vector = GetXY(worldPosition);
            SetTileObject(layer, subLayerIndex, vector.x, vector.y, value);
        }

        public TileObject GetTileObject(TileLayer layer, int subLayerIndex, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                int subLayerOffset = width * height * subLayerIndex;
                return tileGridList[(int)layer].tileObjectsGrid[y * width + x + subLayerOffset];
            }
            else
            {
                return default;
            }
        }

        public TileObject GetTileObject(TileLayer layer, int subLayerIndex, Vector3 worldPosition)
        {
            Vector2Int vector = new Vector2Int();
            vector = GetXY(worldPosition);
            return GetTileObject(layer, subLayerIndex, vector.x, vector.y);
        }

        public void TriggerGridObjectChanged(int x, int y)
        {
            OnGridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArgs { x = x, y = y });
        }

        public void Clear()
        {
            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        TileObject tileObject = GetTileObject(layer, x, y);
                        if (!tileObject.IsEmpty())
                        {
                            tileObject.ClearPlacedObject();
                        }
                    }
                }
            }
        }

        public ChunkSaveObject Save()
        {
            List<TileObject.TileSaveObject> tileObjectSaveObjectList = new List<TileObject.TileSaveObject>();

            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        TileObject tileObject = GetTileObject(layer, x, y);
                        if (!tileObject.IsEmpty())
                        {
                            tileObjectSaveObjectList.Add(tileObject.Save());
                        }
                    }
                }
            }

            ChunkSaveObject saveObject = new ChunkSaveObject {
                tileObjectSaveObjectArray = tileObjectSaveObjectList.ToArray(),
                height = height,
                originPosition = originPosition,
                tileSize = tileSize,
                width = width,
                chunkKey = chunkKey,
            };

            return saveObject;
        }
    }
}