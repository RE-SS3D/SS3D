using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
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
        public event EventHandler<GridObjectChangedEventArgs> OnGridObjectChanged;
        public class GridObjectChangedEventArgs : EventArgs
        {
            public int X;
            public int Y;
        }

        /// <summary>
        /// Grid for grouping TileObjects per layer. Can be used for walking through objects on the same layer fast.
        /// </summary>
        public struct TileGrid
        {
            public TileLayer Layer;
            public TileObject[] TileObjectsGrid;
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
        private readonly Vector2Int _chunkKey;

        private readonly int _width;
        private readonly int _height;
        private readonly float _tileSize;
        private readonly Vector3 _originPosition;
        private List<TileGrid> _tileGridList;
        private TileManager _tileManager;

        public TileChunk(Vector2Int chunkKey, int width, int height, float tileSize, Vector3 originPosition)
        {
            _chunkKey = chunkKey;
            _width = width;
            _height = height;
            _tileSize = tileSize;
            _originPosition = originPosition;

            CreateAllGrids();
            _tileManager = TileManager.Instance;
        }

        /// <summary>
        /// Create a new empty grid for a given layer.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private TileGrid CreateGrid(TileLayer layer)
        {
            TileGrid grid = new TileGrid { Layer = layer };

            int gridSize = _width * _height;
            grid.TileObjectsGrid = new TileObject[gridSize];

            int subLayerSize = TileHelper.GetSubLayerSize(layer);

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    grid.TileObjectsGrid[y * _width + x] = new TileObject(this, layer, x, y, subLayerSize);
                }
            }

            return grid;
        }

        /// <summary>
        /// Create empty grids for all layers.
        /// </summary>
        private void CreateAllGrids()
        {
            _tileGridList = new List<TileGrid>();

            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                _tileGridList.Add(CreateGrid(layer));
            }
        }

        public int GetWidth()
        {
            return _width;
        }

        public int GetHeight()
        {
            return _height;
        }

        public float GetTileSize()
        {
            return _tileSize;
        }

        public Vector3 GetOrigin()
        {
            return _originPosition;
        }

        public Vector2Int GetKey()
        {
            return _chunkKey;
        }

        /// <summary>
        /// Returns the worldposition for a given x and y offset.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x, 0, y) * _tileSize + _originPosition;
        }

        /// <summary>
        /// Returns the x and y offset for a given chunk position.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public Vector2Int GetXY(Vector3 worldPosition)
        {
            return new Vector2Int((int)Math.Round(worldPosition.x - _originPosition.x), (int)Math.Round(worldPosition.z - _originPosition.z));
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
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
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
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
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
            if (x >= 0 && y >= 0 && x < _width && y < _height)
            {
                _tileGridList[(int)layer].TileObjectsGrid[y * _width + x] = value;
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
            if (x >= 0 && y >= 0 && x < _width && y < _height)
            {
                return _tileGridList[(int)layer].TileObjectsGrid[y * _width + x];
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
            Vector2Int vector = GetXY(worldPosition);
            return GetTileObject(layer, vector.x, vector.y);
        }

        public void TriggerGridObjectChanged(int x, int y)
        {
            OnGridObjectChanged?.Invoke(this, new GridObjectChangedEventArgs { X = x, Y = y });
        }

        /// <summary>
        /// Clears the entire chunk of any PlacedTileObject.
        /// </summary>
        public void Clear()
        {
            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
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
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
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
                height = _height,
                originPosition = _originPosition,
                tileSize = _tileSize,
                width = _width,
                chunkKey = _chunkKey,
            };

            return saveObject;
        }
    }
}