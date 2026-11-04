using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// One chunk's worth (<see cref="AtmosConstants.ChunkSize"/> x ChunkSize cells) of GPU
    /// visualization data, sent from the server to clients so pure clients can build the same
    /// atlas <see cref="AtmosGpuUploader"/> builds on the host. See
    /// Documents/architecture/2026-07_atmos-client-visualization-sync.md.
    /// </summary>
    public struct AtmosChunkPatch
    {
        public const int CellCount = AtmosConstants.CellsPerChunk;

        /// <summary>Serialized size of one patch: map id, chunk key, then the per-cell channels.</summary>
        public const int WireBytes = 12 + CellCount * (2 + 2 + 4 + 2 + 1 + 1);

        public int MapId;

        /// <summary>Chunk grid key (tile coord / ChunkSize), matching <see cref="TileChunkRef.ChunkKey"/>.</summary>
        public Vector2Int ChunkKey;

        public float[] Pressure;
        public float[] Temperature;
        public Color32[] Composition;

        /// <summary>Packed 0..1 wind, 2 floats (x, z) per cell; see <see cref="AtmosCellVisualEncoding.PackFlow"/>.</summary>
        public float[] Flow;
        public float[] FireIntensity;
        public byte[] Mask;
    }
}
