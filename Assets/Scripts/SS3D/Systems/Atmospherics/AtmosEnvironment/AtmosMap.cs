using SS3D.Core;
using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public class AtmosMap
    {
        /// <summary>
        /// Number of TileAtmosObjects that should go in a chunk. 16 x 16
        /// </summary>
        public static readonly int ChunkSize = 16;

        /// <summary>
        /// The size of each tile.
        /// </summary>
        private const float TileSize = 1.0f;

        private Dictionary<Vector2Int, AtmosChunk> _atmosChunks;

        private TileMap _tileMap;

        private string _mapName;

        private AtmosEnvironmentSubSystem _atmosEnvironmentSubSystem;

        public AtmosMap(TileMap tileMap, string name)
        {
            _atmosChunks = new Dictionary<Vector2Int, AtmosChunk>();
            _atmosEnvironmentSubSystem = Subsystems.Get<AtmosEnvironmentSubSystem>();
            _mapName = name;
            _tileMap = tileMap;
        }

        public int ChunkCount { get => _atmosChunks.Count; }

        public AtmosContainer GetTileAtmosObject(Vector3 worldPosition)
        {
            /*
            AtmosChunk chunk = GetOrCreateAtmosChunk(worldPosition);
            return chunk.GetTileAtmosObject(worldPosition);
            */

            Vector2Int key = GetKey(worldPosition);
            AtmosChunk chunk;

            if (!_atmosChunks.TryGetValue(key, out chunk))
            {
                return null;
            }

            return chunk.GetTileAtmosObject(worldPosition);
        }

        public string GetName()
        {
            return _mapName;
        }

        public void SetName(string name)
        {
            _mapName = name;
        }

        public void CreateChunkFromTileChunk(Vector2Int chunkKey, Vector3 origin)
        {
            _atmosChunks[chunkKey] = CreateChunk(chunkKey, origin);
        }

        public TileMap GetLinkedTileMap()
        {
            return _tileMap;
        }

        /// <summary>
        /// Clears the entire map.
        /// </summary>
        public void Clear()
        {
            _atmosChunks.Clear();
        }

        public AtmosChunk GetChunk(Vector3 worldPosition)
        {
            Vector2Int key = GetKey(worldPosition);
            if (!_atmosChunks.TryGetValue(key, out AtmosChunk chunk))
            {
                return null;
            }

            return chunk;
        }

        public AtmosChunk[] GetAtmosChunks()
        {
            return _atmosChunks?.Values.ToArray();
        }

        public AtmosChunk[] GetChunkAndEightNeighbours(Vector3 worldPosition)
        {
            AtmosChunk[] chunks = new AtmosChunk[9];
            chunks[0] = GetChunk(worldPosition);                                                               // center
            chunks[1] = GetChunk(worldPosition + (Vector3.forward * ChunkSize));                               // north chunk
            chunks[2] = GetChunk(worldPosition + (Vector3.forward * ChunkSize) + (Vector3.right * ChunkSize)); // north east chunk
            chunks[3] = GetChunk(worldPosition + (Vector3.right * ChunkSize));                                 // east chunk
            chunks[4] = GetChunk(worldPosition - (Vector3.forward * ChunkSize) + (Vector3.right * ChunkSize)); // south east chunk
            chunks[5] = GetChunk(worldPosition - (Vector3.forward * ChunkSize));                               // south chunk
            chunks[6] = GetChunk(worldPosition - (Vector3.forward * ChunkSize) - (Vector3.right * ChunkSize)); // south west chunk
            chunks[7] = GetChunk(worldPosition - (Vector3.right * ChunkSize));                                 // west chunk
            chunks[8] = GetChunk(worldPosition + (Vector3.forward * ChunkSize) - (Vector3.right * ChunkSize)); // north west chunk

            return chunks;
        }

        /// <summary>
        /// Create a new atmos chunk.
        /// </summary>
        /// <param name="chunkKey">Unique key to use</param>
        /// <param name="origin">Origin position of the chunk</param>
        /// <returns></returns>
        private AtmosChunk CreateChunk(Vector2Int chunkKey, Vector3 origin)
        {
            AtmosChunk chunk = new(this, chunkKey, ChunkSize, ChunkSize, TileSize, origin);

            return chunk;
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
        private Vector2Int GetKey(Vector3 worldPosition)
        {
            int x = (int)Math.Floor(worldPosition.x / ChunkSize);
            int y = (int)Math.Floor(worldPosition.z / ChunkSize);

            return GetKey(x, y);
        }

        /// <summary>
        /// Returns the tile chunk based the world position. Will create a new chunk if it doesn't exist.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        private AtmosChunk GetOrCreateAtmosChunk(Vector3 worldPosition)
        {
            Vector2Int key = GetKey(worldPosition);
            AtmosChunk chunk;

            // Create a new chunk if there is none
            if (!_atmosChunks.TryGetValue(key, out chunk))
            {
                Vector3 origin = new Vector3 { x = key.x * ChunkSize, z = key.y * ChunkSize };
                _atmosChunks[key] = CreateChunk(key, origin);
            }

            return _atmosChunks[key];
        }
    }
}
