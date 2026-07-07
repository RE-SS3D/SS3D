using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Atmospherics.ECS.Jobs;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Server-side turf gas grid. Owns native buffers and schedules ShareGasJob.
    /// </summary>
    public sealed class AtmosSimulation : System.IDisposable
    {
        private readonly ITileQueryService _query;
        private readonly int _mapId;
        private readonly int _gasTypeCount;

        private readonly List<TileChunkRef> _chunks = new();
        private readonly Dictionary<TileCoord, int> _coordToIndex = new();
        private readonly Dictionary<Vector2Int, int> _chunkKeyToIndex = new();

        private NativeArray<float> _molesRead;
        private NativeArray<float> _molesWrite;
        private NativeArray<AtmosCellMeta> _cellMeta;
        private NativeArray<AtmosCellMeta> _cellMetaWrite;
        private NativeArray<AtmosNeighbours> _neighbours;
        private NativeList<int> _activeCells;

        private int _cellCount;

        public int CellCount => _cellCount;
        public int ActiveCellCount => _activeCells.IsCreated ? _activeCells.Length : 0;

        public AtmosSimulation(ITileQueryService query, int mapId, int gasTypeCount)
        {
            _query = query;
            _mapId = mapId;
            _gasTypeCount = gasTypeCount;
            _activeCells = new NativeList<int>(Allocator.Persistent);
        }

        public void CreateChunk(TileChunkRef chunkRef)
        {
            if (_chunkKeyToIndex.ContainsKey(chunkRef.ChunkKey))
                return;

            int chunkIndex = _chunks.Count;
            _chunks.Add(chunkRef);
            _chunkKeyToIndex[chunkRef.ChunkKey] = chunkIndex;

            int baseIndex = _cellCount;
            _cellCount += AtmosConstants.CellsPerChunk;
            ResizeBuffers(_cellCount);

            for (int localY = 0; localY < AtmosConstants.ChunkSize; localY++)
            {
                for (int localX = 0; localX < AtmosConstants.ChunkSize; localX++)
                {
                    int localIndex = localY * AtmosConstants.ChunkSize + localX;
                    int globalIndex = baseIndex + localIndex;
                    int worldX = chunkRef.ChunkKey.x * AtmosConstants.ChunkSize + localX;
                    int worldY = chunkRef.ChunkKey.y * AtmosConstants.ChunkSize + localY;
                    var coord = new TileCoord(chunkRef.MapId, worldX, worldY);
                    _coordToIndex[coord] = globalIndex;
                    InitCell(globalIndex, coord);
                }
            }

            RebuildChunkNeighbours(baseIndex);
        }

        public void UpdateCell(TileCoord coord)
        {
            if (!_coordToIndex.TryGetValue(coord, out int cellIndex))
                return;

            InitCell(cellIndex, coord);
            RebuildNeighboursAround(coord);
            ActivateRegion(coord, 1);
        }

        public void ActivateRegion(TileCoord coord, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var neighbourCoord = new TileCoord(coord.MapId, coord.Grid.x + dx, coord.Grid.y + dy);
                    if (!_coordToIndex.TryGetValue(neighbourCoord, out int cellIndex))
                        continue;

                    AtmosCellMeta meta = _cellMeta[cellIndex];
                    if (meta.State is AtmosCellState.Blocked or AtmosCellState.Vacuum)
                        continue;

                    meta.State = AtmosCellState.Active;
                    _cellMeta[cellIndex] = meta;
                }
            }
        }

        public void Tick(float deltaTime)
        {
            if (_cellCount == 0)
                return;

            RebuildActiveList();
            if (_activeCells.Length == 0)
                return;

            int substeps = GetBreachSubsteps();
            float subDelta = deltaTime / substeps;

            for (int step = 0; step < substeps; step++)
                RunShareGasJob(subDelta);
        }

        public bool TryGetCellDebugInfo(TileCoord coord, out AtmosCellDebugInfo info)
        {
            info = default;
            if (!_coordToIndex.TryGetValue(coord, out int cellIndex))
                return false;

            AtmosCellMeta meta = _cellMeta[cellIndex];
            _query.TryGetOccupancy(coord, out TileOccupancy occupancy);

            info = new AtmosCellDebugInfo
            {
                Exists = true,
                Coord = coord,
                State = meta.State,
                Temperature = meta.Temperature,
                Volume = meta.Volume,
                Pressure = GetPressure(cellIndex, meta),
                Neighbours = _neighbours[cellIndex],
                Occupancy = occupancy,
            };
            return true;
        }

        public float GetTotalMoles()
        {
            float total = 0f;
            for (int i = 0; i < _molesRead.Length; i++)
                total += _molesRead[i];
            return total;
        }

        public void Dispose()
        {
            if (_molesRead.IsCreated) _molesRead.Dispose();
            if (_molesWrite.IsCreated) _molesWrite.Dispose();
            if (_cellMeta.IsCreated) _cellMeta.Dispose();
            if (_cellMetaWrite.IsCreated) _cellMetaWrite.Dispose();
            if (_neighbours.IsCreated) _neighbours.Dispose();
            if (_activeCells.IsCreated) _activeCells.Dispose();
        }

        private void InitCell(int cellIndex, TileCoord coord)
        {
            if (!_query.TryGetOccupancy(coord, out TileOccupancy occupancy))
                return;

            ClearMoles(cellIndex);

            var meta = new AtmosCellMeta
            {
                Volume = AtmosConstants.CellVolume,
                Temperature = AtmosConstants.StandardTemperature,
                State = AtmosCellState.Inactive,
            };

            if (!occupancy.HasPlenum)
            {
                meta.Temperature = AtmosConstants.SpaceTemperature;
                meta.State = AtmosCellState.Vacuum;
                _cellMeta[cellIndex] = meta;
                return;
            }

            SetMoles(cellIndex, AtmosConstants.Oxygen, AtmosConstants.StationOxygenMoles);
            SetMoles(cellIndex, AtmosConstants.Nitrogen, AtmosConstants.StationNitrogenMoles);
            meta.State = AtmosCellState.Active;

            if (occupancy.HasWall && occupancy.IsAirtight)
                meta.State = AtmosCellState.Blocked;

            _cellMeta[cellIndex] = meta;
        }

        private void RebuildChunkNeighbours(int baseIndex)
        {
            for (int localIndex = 0; localIndex < AtmosConstants.CellsPerChunk; localIndex++)
            {
                int cellIndex = baseIndex + localIndex;
                TileCoord coord = IndexToCoord(cellIndex);
                AtmosNeighbourBuilder.RebuildNeighbours(cellIndex, coord, _coordToIndex, _query, _neighbours);
            }
        }

        private void RebuildNeighboursAround(TileCoord coord)
        {
            RebuildNeighboursForCoord(coord);

            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                Direction direction = directionIndex switch
                {
                    0 => Direction.North,
                    1 => Direction.East,
                    2 => Direction.South,
                    _ => Direction.West,
                };
                TileCoord neighbourCoord = AtmosNeighbourBuilder.GetNeighbourCoord(coord, direction);
                RebuildNeighboursForCoord(neighbourCoord);
            }
        }

        private void RebuildNeighboursForCoord(TileCoord coord)
        {
            if (!_coordToIndex.TryGetValue(coord, out int cellIndex))
                return;

            AtmosNeighbourBuilder.RebuildNeighbours(cellIndex, coord, _coordToIndex, _query, _neighbours);
        }

        private void RebuildActiveList()
        {
            _activeCells.Clear();
            for (int i = 0; i < _cellCount; i++)
            {
                if (_cellMeta[i].IsSimulated)
                    _activeCells.Add(i);
            }
        }

        private int GetBreachSubsteps()
        {
            float maxDeltaPressure = 0f;
            for (int i = 0; i < _activeCells.Length; i++)
            {
                int cellIndex = _activeCells[i];
                AtmosCellMeta self = _cellMeta[cellIndex];
                float selfPressure = GetPressure(cellIndex, self);

                for (int direction = 0; direction < 4; direction++)
                {
                    int neighbourIndex = _neighbours[cellIndex].Get(direction);
                    if (neighbourIndex < 0)
                        continue;

                    float neighbourPressure = GetPressure(neighbourIndex, _cellMeta[neighbourIndex]);
                    maxDeltaPressure = Mathf.Max(maxDeltaPressure, Mathf.Abs(selfPressure - neighbourPressure));
                }
            }

            if (maxDeltaPressure <= AtmosFluxConstants.BreachPressureThreshold)
                return 1;

            return Mathf.Clamp(
                Mathf.CeilToInt(maxDeltaPressure / AtmosFluxConstants.BreachPressureThreshold),
                1,
                AtmosFluxConstants.MaxBreachSubsteps);
        }

        private void RunShareGasJob(float deltaTime)
        {
            var job = new ShareGasJob
            {
                ActiveCells = _activeCells.AsArray(),
                MolesRead = _molesRead,
                MolesWrite = _molesWrite,
                CellMeta = _cellMeta,
                CellMetaWrite = _cellMetaWrite,
                Neighbours = _neighbours,
                MaxGasTypes = AtmosConstants.MaxGasTypes,
                GasTypeCount = _gasTypeCount,
                DeltaTime = deltaTime,
            };

            job.Schedule().Complete();
            SwapSimulationBuffers();
        }

        private void SwapSimulationBuffers()
        {
            (_molesRead, _molesWrite) = (_molesWrite, _molesRead);
            (_cellMeta, _cellMetaWrite) = (_cellMetaWrite, _cellMeta);
        }

        private void ResizeBuffers(int cellCount)
        {
            int moleStride = cellCount * AtmosConstants.MaxGasTypes;
            ResizeNativeArray(ref _molesRead, moleStride);
            ResizeNativeArray(ref _molesWrite, moleStride);
            ResizeNativeArray(ref _cellMeta, cellCount);
            ResizeNativeArray(ref _cellMetaWrite, cellCount);
            ResizeNativeArray(ref _neighbours, cellCount);
        }

        private static void ResizeNativeArray<T>(ref NativeArray<T> array, int length) where T : struct
        {
            if (array.IsCreated)
                array.Dispose();

            if (length > 0)
                array = new NativeArray<T>(length, Allocator.Persistent);
        }

        private void ClearMoles(int cellIndex)
        {
            int baseIndex = cellIndex * AtmosConstants.MaxGasTypes;
            for (int gasId = 0; gasId < AtmosConstants.MaxGasTypes; gasId++)
            {
                _molesRead[baseIndex + gasId] = 0f;
                _molesWrite[baseIndex + gasId] = 0f;
            }
        }

        private void SetMoles(int cellIndex, GasId gasId, float moles)
        {
            _molesRead[GasMixture.GetMoleIndex(cellIndex, gasId)] = moles;
            _molesWrite[GasMixture.GetMoleIndex(cellIndex, gasId)] = moles;
        }

        private float GetPressure(int cellIndex, AtmosCellMeta meta)
        {
            if (meta.Volume <= 0f || meta.Temperature <= 0f)
                return 0f;

            float totalMoles = 0f;
            int baseIndex = cellIndex * AtmosConstants.MaxGasTypes;
            for (int gasId = 0; gasId < _gasTypeCount; gasId++)
                totalMoles += _molesRead[baseIndex + gasId];

            return totalMoles * AtmosFluxConstants.GasConstant * meta.Temperature / meta.Volume / 1000f;
        }

        private TileCoord IndexToCoord(int cellIndex)
        {
            foreach (KeyValuePair<TileCoord, int> pair in _coordToIndex)
            {
                if (pair.Value == cellIndex)
                    return pair.Key;
            }

            return new TileCoord(_mapId, 0, 0);
        }
    }
}
