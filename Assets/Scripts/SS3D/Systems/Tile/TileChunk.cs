using SS3D.Core;
using SS3D.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    public class TileChunk
    {
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

        public TileChunk(Vector2Int chunkKey, int width, int height, float tileSize, Vector3 originPosition)
        {
            _chunkKey = chunkKey;
            _width = width;
            _height = height;
            _tileSize = tileSize;
            _originPosition = originPosition;

            CreateAllGrids();
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


            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    grid.TileObjectsGrid[y * _width + x] = new TileObject(layer, x, y);
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
            }
            else
            {
                Punpun.Yell(SystemLocator.Get<TileSystem>(), "Tried to set tile object outside of chunk boundary");
            }
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
                Punpun.Yell(SystemLocator.Get<TileSystem>(), "Tried to get tile object outside of chunk boundary");
                return default;
            }
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
                        tileObject.ClearPlacedObject();
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
            var tileObjectSaveObjectList = new List<TileObject.TileSaveObject>();

            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        TileObject tileObject = GetTileObject(layer, x, y);
                        if (!tileObject.IsEmpty())
                        {
                            tileObjectSaveObjectList.Add(tileObject.Save());
                        }
                    }
                }
            }

            ChunkSaveObject saveObject = new ChunkSaveObject
            {
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