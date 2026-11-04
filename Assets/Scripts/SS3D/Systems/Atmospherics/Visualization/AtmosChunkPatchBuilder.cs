using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using System;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Builds a single-chunk <see cref="AtmosChunkPatch"/> straight from the authoritative
    /// simulation, mirroring the per-texel encoding <see cref="AtmosGpuUploader"/> uses so a
    /// network client's atlas matches the host's. Server-only.
    /// Reuses patch buffers across builds — FishNet serializes synchronously in
    /// <c>RpcApplyChunkPatch</c> before the next build overwrites them.
    /// </summary>
    public sealed class AtmosChunkPatchBuilder
    {
        private float[] _visualFire = Array.Empty<float>();
        private readonly float[] _pressure = new float[AtmosChunkPatch.CellCount];
        private readonly float[] _temperature = new float[AtmosChunkPatch.CellCount];
        private readonly Color32[] _composition = new Color32[AtmosChunkPatch.CellCount];
        private readonly float[] _flow = new float[AtmosChunkPatch.CellCount * 2];
        private readonly float[] _fireIntensity = new float[AtmosChunkPatch.CellCount];
        private readonly byte[] _mask = new byte[AtmosChunkPatch.CellCount];

        public AtmosChunkPatch Build(AtmosSimulation simulation, int chunkIndex, Vector2Int chunkKey)
        {
            EnsureCapacity(simulation.CellCount);

            var patch = new AtmosChunkPatch
            {
                MapId = simulation.MapId,
                ChunkKey = chunkKey,
                Pressure = _pressure,
                Temperature = _temperature,
                Composition = _composition,
                Flow = _flow,
                FireIntensity = _fireIntensity,
                Mask = _mask,
            };

            int baseIndex = chunkIndex * AtmosConstants.CellsPerChunk;

            for (int localZ = 0; localZ < AtmosConstants.ChunkSize; localZ++)
            {
                for (int localX = 0; localX < AtmosConstants.ChunkSize; localX++)
                {
                    int local = localZ * AtmosConstants.ChunkSize + localX;
                    int cellIndex = baseIndex + local;
                    var coord = new TileCoord(
                        simulation.MapId,
                        chunkKey.x * AtmosConstants.ChunkSize + localX,
                        chunkKey.y * AtmosConstants.ChunkSize + localZ);

                    BuildCell(simulation, patch, cellIndex, local);
                }
            }

            return patch;
        }

        private void BuildCell(AtmosSimulation simulation, AtmosChunkPatch patch, int cellIndex, int local)
        {
            AtmosCellMeta meta = simulation.CellMeta[cellIndex];
            float pressure = simulation.GetCellPressure(cellIndex);

            float simFire = simulation.BurnIntensity.IsCreated ? simulation.BurnIntensity[cellIndex] : 0f;
            float decayedFire = _visualFire[cellIndex] * AtmosVisualMetrics.VisualFireDecayPerTick;
            float visualFire = Mathf.Max(simFire, decayedFire);
            _visualFire[cellIndex] = visualFire;

            patch.Pressure[local] = pressure;
            patch.Temperature[local] = meta.Temperature;
            patch.FireIntensity[local] = visualFire;
            patch.Mask[local] = AtmosCellVisualEncoding.EncodeMask(meta.State);
            patch.Composition[local] = AtmosCellVisualEncoding.EncodeComposition(simulation.MolesRead, cellIndex);

            AtmosCellVisualEncoding.PackFlow(
                AtmosCellVisualEncoding.EncodeFlow(AtmosWind.SampleCell(simulation, cellIndex)), patch.Flow, local);
        }

        private void EnsureCapacity(int cellCount)
        {
            if (_visualFire.Length >= cellCount)
                return;

            var grown = new float[cellCount];
            Array.Copy(_visualFire, grown, _visualFire.Length);
            _visualFire = grown;
        }
    }
}
