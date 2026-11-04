using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Tracks which atmos chunks changed enough visually (pressure, temperature, composition, wind,
    /// fire, or state) to be worth re-sending to clients. Reused per tick so only chunks with a
    /// meaningful delta get a network patch, instead of broadcasting the whole map. Server-only.
    /// Scans <see cref="AtmosSimulation.ActiveCells"/> only — stagnant Inactive/Vacuum/Blocked
    /// tiles do not wake the dirty set. A cell compares against the values it last reported, not
    /// last tick's: a big room's slow drain changes each tick by less than any threshold, and
    /// clients would never hear of it. Chunks queue oldest first, so a cap on chunks per tick
    /// delays changes evenly instead of starving a chunk.
    /// </summary>
    public sealed class AtmosDirtyChunkTracker
    {
        private const float PressureEpsilonKpa = 0.5f;
        private const float TemperatureEpsilonKelvin = 0.5f;
        private const float FireEpsilon = 0.02f;
        private const float CompositionEpsilon = 0.02f;

        // Encoded wind, 0..1. Encoding follows the square root of speed, so a draught dying away counts.
        private const float FlowEpsilon = 0.04f;
        private const int CompositionChannels = AtmosCellVisualEncoding.CompositionGasChannels;

        private float[] _prevPressure = Array.Empty<float>();
        private float[] _prevTemperature = Array.Empty<float>();
        private float[] _prevFire = Array.Empty<float>();
        private Vector2[] _prevFlow = Array.Empty<Vector2>();
        private float[] _prevComposition = Array.Empty<float>();
        private byte[] _prevMask = Array.Empty<byte>();

        private readonly HashSet<Vector2Int> _dirtyChunks = new();
        private readonly Queue<Vector2Int> _dirtyOrder = new();

        public void Update(AtmosSimulation simulation)
        {
            if (simulation == null || simulation.CellCount == 0)
                return;

            EnsureCapacity(simulation.CellCount);

            NativeArray<int> activeCells = simulation.ActiveCells;
            // Tick rebuilds the active list; EditMode tests may call Update without Tick.
            if (!activeCells.IsCreated || activeCells.Length == 0)
            {
                UpdateAllChunkCells(simulation);
                return;
            }

            IReadOnlyList<TileChunkRef> chunks = simulation.Chunks;
            for (int i = 0; i < activeCells.Length; i++)
            {
                int cellIndex = activeCells[i];
                if (!UpdateCell(simulation, cellIndex))
                    continue;

                int chunkIndex = cellIndex / AtmosConstants.CellsPerChunk;
                if (chunkIndex >= 0 && chunkIndex < chunks.Count)
                    MarkDirty(chunks[chunkIndex].ChunkKey);
            }
        }

        private void UpdateAllChunkCells(AtmosSimulation simulation)
        {
            IReadOnlyList<TileChunkRef> chunks = simulation.Chunks;
            for (int chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
            {
                int baseIndex = chunkIndex * AtmosConstants.CellsPerChunk;
                bool dirty = false;

                for (int local = 0; local < AtmosConstants.CellsPerChunk; local++)
                {
                    if (UpdateCell(simulation, baseIndex + local))
                        dirty = true;
                }

                if (dirty)
                    MarkDirty(chunks[chunkIndex].ChunkKey);
            }
        }

        /// <summary>
        /// Moves up to <paramref name="maxChunks"/> of the longest-waiting dirty chunks into
        /// <paramref name="results"/>. The rest stay queued for the next call.
        /// </summary>
        public void ConsumeDirtyChunks(List<Vector2Int> results, int maxChunks = int.MaxValue)
        {
            results.Clear();
            while (results.Count < maxChunks && _dirtyOrder.Count > 0)
            {
                Vector2Int chunk = _dirtyOrder.Dequeue();
                _dirtyChunks.Remove(chunk);
                results.Add(chunk);
            }
        }

        private void MarkDirty(Vector2Int chunk)
        {
            if (_dirtyChunks.Add(chunk))
                _dirtyOrder.Enqueue(chunk);
        }

        private bool UpdateCell(AtmosSimulation simulation, int cellIndex)
        {
            AtmosCellMeta meta = simulation.CellMeta[cellIndex];
            byte mask = AtmosCellVisualEncoding.EncodeMask(meta.State);
            bool changed = mask != _prevMask[cellIndex];
            _prevMask[cellIndex] = mask;

            // Blocked/vacuum/inactive cells don't drive visuals beyond their mask; skip the
            // more expensive per-channel comparisons for them.
            if (!meta.IsSimulated)
                return changed;

            if (Changed(simulation.GetCellPressure(cellIndex), ref _prevPressure[cellIndex], PressureEpsilonKpa))
                changed = true;

            if (Changed(meta.Temperature, ref _prevTemperature[cellIndex], TemperatureEpsilonKelvin))
                changed = true;

            float fire = simulation.BurnIntensity.IsCreated ? simulation.BurnIntensity[cellIndex] : 0f;
            if (Changed(fire, ref _prevFire[cellIndex], FireEpsilon))
                changed = true;

            Vector2 flow = AtmosCellVisualEncoding.EncodeFlow(AtmosWind.SampleCell(simulation, cellIndex));
            if ((flow - _prevFlow[cellIndex]).sqrMagnitude > FlowEpsilon * FlowEpsilon)
            {
                _prevFlow[cellIndex] = flow;
                changed = true;
            }

            if (UpdateComposition(simulation, cellIndex))
                changed = true;

            return changed;
        }

        private static bool Changed(float value, ref float reported, float epsilon)
        {
            if (Mathf.Abs(value - reported) <= epsilon)
                return false;

            reported = value;
            return true;
        }

        private bool UpdateComposition(AtmosSimulation simulation, int cellIndex)
        {
            int compositionBase = cellIndex * CompositionChannels;
            float totalMoles = 0f;
            Span<float> fractions = stackalloc float[CompositionChannels];

            for (int gasId = 0; gasId < CompositionChannels; gasId++)
            {
                fractions[gasId] = simulation.MolesRead[GasMixture.GetMoleIndex(cellIndex, new GasId((ushort)gasId))];
                totalMoles += fractions[gasId];
            }

            if (totalMoles > 1e-6f)
            {
                for (int gasId = 0; gasId < CompositionChannels; gasId++)
                    fractions[gasId] /= totalMoles;
            }

            bool changed = false;
            for (int gasId = 0; gasId < CompositionChannels; gasId++)
            {
                if (Changed(fractions[gasId], ref _prevComposition[compositionBase + gasId], CompositionEpsilon))
                    changed = true;
            }

            return changed;
        }

        private void EnsureCapacity(int cellCount)
        {
            if (_prevMask.Length >= cellCount)
                return;

            Grow(ref _prevPressure, cellCount);
            Grow(ref _prevTemperature, cellCount);
            Grow(ref _prevFire, cellCount);
            Grow(ref _prevFlow, cellCount);
            Grow(ref _prevMask, cellCount);
            Grow(ref _prevComposition, cellCount * CompositionChannels);
        }

        private static void Grow<T>(ref T[] array, int length)
        {
            var grown = new T[length];
            Array.Copy(array, grown, array.Length);
            array = grown;
        }
    }
}
