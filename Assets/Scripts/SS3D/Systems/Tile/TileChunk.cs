using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles
{
    /// <summary>
    /// Chunk class used for grouping together TileObjects.
    /// 
    /// One dimensional arrays are used for 2 dimensional grids that can be addressed via [y * width + x]
    /// 
    /// </summary>
    public class TileChunk
    {
        /// <summary>
        /// Event that is triggered when a TileObject changes.
        /// </summary>
        public event EventHandler<OnGridObjectChangedEventArgs> OnGridObjectChanged;
        public class OnGridObjectChangedEventArgs : EventArgs
        {
            public int x;
            public int y;
        }

        /// <summary>
        /// Grid for grouping TileObjects per layer. Can be used for walking through objects on the same layer fast.
        /// </summary>
        public struct TileGrid
        {
            public TileLayer layer;
            public TileObject[] tileObjectsGrid;
        }

        /// <summary>
        /// SaveObject used by chunks.
        /// </summary>
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

        /// <summary>
        /// Unique key for each chunk
        /// </summary>
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

        /// <summary>
        /// Create a new empty grid for a given layer.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private TileGrid CreateGrid(TileLayer layer)
        {
            TileGrid grid = new TileGrid { layer = layer };

            int gridSize = width * height;
            grid.tileObjectsGrid = new TileObject[gridSize];

            int subLayerSize = TileHelper.GetSubLayerSize(layer);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid.tileObjectsGrid[y * width + x] = new TileObject(this, layer, x, y, subLayerSize);
                }
            }

            return grid;
        }

        /// <summary>
        /// Create empty grids for all layers.
        /// </summary>
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

        /// <summary>
        /// Returns the worldposition for a given x and y offset.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x, 0, y) * tileSize + originPosition;
        }

        /// <summary>
        /// Returns the x and y offset for a given chunk position.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public Vector2Int GetXY(Vector3 worldPosition)
        {
            return new Vector2Int((int)Math.Round(worldPosition.x - originPosition.x), (int)Math.Round(worldPosition.z - originPosition.z));
        }

        /// <summary>
        /// Determines if all layers in the chunk are completely empty.
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            bool empty = true;

            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        TileObject tileObject = GetTileObject(layer, x, y);
                        if (!tileObject.IsCompletelyEmpty())
                            empty = false;
                    }
                }
            }

            return empty;
        }

        /// <summary>
        /// Sets all gameobjects for a given layer to either enabled or disabled.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="enabled"></param>
        public void SetEnabled(TileLayer layer, bool enabled)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int i = 0; i < TileHelper.GetSubLayerSize(layer); i++)
                    {
                        GetTileObject(layer, x, y).GetPlacedObject(i)?.gameObject.SetActive(enabled);
                    }
                }
            }
        }

        /// <summary>
        /// Sets a TileObject value for a given x and y.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public void SetTileObject(TileLayer layer, int x, int y, TileObject value)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                tileGridList[(int)layer].tileObjectsGrid[y * width + x] = value;
                TriggerGridObjectChanged(x, y);
            }
        }

        /// <summary>
        /// Sets a TileObject value for a given worldposition.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="worldPosition"></param>
        /// <param name="value"></param>
        public void SetTileObject(TileLayer layer, Vector3 worldPosition, TileObject value)
        {
            Vector2Int vector = GetXY(worldPosition);
            SetTileObject(layer, vector.x, vector.y, value);
        }

        /// <summary>
        /// Gets a TileObject value for a given x and y.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public TileObject GetTileObject(TileLayer layer, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                return tileGridList[(int)layer].tileObjectsGrid[y * width + x];
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Gets a TileObject value for a given worldposition.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public TileObject GetTileObject(TileLayer layer, Vector3 worldPosition)
        {
            Vector2Int vector = new Vector2Int();
            vector = GetXY(worldPosition);
            return GetTileObject(layer, vector.x, vector.y);
        }

        public void TriggerGridObjectChanged(int x, int y)
        {
            OnGridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArgs { x = x, y = y });
        }

        /// <summary>
        /// Clears the entire chunk of any PlacedTileObject.
        /// </summary>
        public void Clear()
        {
            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        TileObject tileObject = GetTileObject(layer, x, y);
                        for (int i = 0; i < TileHelper.GetSubLayerSize(layer); i++)
                        {
                            if (!tileObject.IsEmpty(i))
                            {
                                tileObject.ClearPlacedObject(i);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves all the TileObjects in the chunk.
        /// </summary>
        /// <returns></returns>
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
                        if (!tileObject.IsCompletelyEmpty())
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