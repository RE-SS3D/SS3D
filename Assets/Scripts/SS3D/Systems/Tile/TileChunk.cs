using FishNet;
using FishNet.Object;
using SS3D.Core;
using SS3D.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Class for holding a 16x16 grid of TileObjects.
    /// </summary>
    public class TileChunk: NetworkBehaviour
    {
        /// <summary>
        /// Number of TileObjects that should go in a chunk. 16 x 16
        /// </summary>
        public const int ChunkSize = 16;

        /// <summary>
        /// Grid for grouping TileObjects per layer. Can be used for walking through objects on the same layer fast.
        /// </summary>
        private struct TileGrid
        {
            public TileLayer Layer;
            public TileObject[] TileObjectsGrid;
        }

        /// <summary>
        /// Unique key for each chunk
        /// </summary>
        private Vector2Int _chunkKey;
        private Vector3 _originPosition;
        private List<TileGrid> _tileGridList;

        public static TileChunk Create(Vector2Int chunkKey, Vector3 originPosition)
        {
            GameObject chunkObject = new GameObject($"Chunk [{originPosition.x},{originPosition.z}]" );
            TileChunk chunk = chunkObject.AddComponent<TileChunk>();

            chunk.Setup(chunkKey, originPosition);

            if (InstanceFinder.ServerManager != null && chunkObject.GetComponent<NetworkObject>() != null)
            {
                InstanceFinder.ServerManager.Spawn(chunkObject);
            }

            return chunk;
        }

        private void Setup(Vector2Int chunkKey, Vector3 originPosition)
        {
            _chunkKey = chunkKey;
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

            int gridSize = ChunkSize * ChunkSize;
            grid.TileObjectsGrid = new TileObject[gridSize];


            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    grid.TileObjectsGrid[y * ChunkSize + x] = new TileObject(layer, x, y);
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
            return new Vector3(x, 0, y) + _originPosition;
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


        public void SetTileObject(TileLayer layer, int x, int y, TileObject value)
        {
            if (x >= 0 && y >= 0 && x < ChunkSize && y < ChunkSize)
            {
                _tileGridList[(int)layer].TileObjectsGrid[y * ChunkSize + x] = value;
            }
            else
            {
                Punpun.Yell(SystemLocator.Get<TileSystem>(), "Tried to set tile object outside of chunk boundary");
            }
        }

        public TileObject GetTileObject(TileLayer layer, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < ChunkSize && y < ChunkSize)
            {
                return _tileGridList[(int)layer].TileObjectsGrid[y * ChunkSize + x];
            }
            else
            {
                Punpun.Yell(SystemLocator.Get<TileSystem>(), "Tried to get tile object outside of chunk boundary");
                return default;
            }
        }

        public TileObject GetTileObject(TileLayer layer, Vector3 worldPosition)
        {
            Vector2Int vector = GetXY(worldPosition);
            return GetTileObject(layer, vector.x, vector.y);
        }

        /// <summary>
        /// Clears the entire chunk of any PlacedTileObject.
        /// </summary>
        public void Clear()
        {
            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                for (int x = 0; x < ChunkSize; x++)
                {
                    for (int y = 0; y < ChunkSize; y++)
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
        public SavedTileChunk Save()
        {
            var tileObjectSaveObjectList = new List<SavedTileObject>();

            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                for (int x = 0; x < ChunkSize; x++)
                {
                    for (int y = 0; y < ChunkSize; y++)
                    {
                        TileObject tileObject = GetTileObject(layer, x, y);
                        if (!tileObject.IsEmpty)
                        {
                            tileObjectSaveObjectList.Add(tileObject.Save());
                        }
                    }
                }
            }

            SavedTileChunk saveObject = new SavedTileChunk
            {
                tileObjectSaveObjectArray = tileObjectSaveObjectList.ToArray(),
                originPosition = _originPosition,
                chunkKey = _chunkKey,
            };

            return saveObject;
        }
    }
}