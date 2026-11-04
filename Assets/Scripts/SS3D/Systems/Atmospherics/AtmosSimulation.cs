using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Atmospherics.Pipes;
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
        private readonly HashSet<TileCoord> _pendingRefresh = new();
        private readonly HashSet<int> _pendingActivate = new();
        private readonly HashSet<int> _setScratch = new();

        private NativeArray<float> _molesRead;
        private NativeArray<float> _molesWrite;
        private NativeArray<AtmosCellMeta> _cellMeta;
        private NativeArray<AtmosCellMeta> _cellMetaWrite;
        private NativeArray<AtmosNeighbours> _neighbours;
        private NativeArray<float> _specificHeats;
        private NativeArray<float> _molarMasses;
        private NativeArray<float> _energyScratch;
        private NativeArray<float> _burnIntensity;
        private NativeArray<AtmosEdgeFlow> _edgeFlow;
        private NativeArray<AtmosEdgeCellScratch> _edgeScratch;
        private NativeList<int> _activeCells;
        private NativeList<int> _workingSet;

        private int _cellCount;

        public int CellCount => _cellCount;
        public int ActiveCellCount => _activeCells.IsCreated ? _activeCells.Length : 0;
        public int GasTypeCount => _gasTypeCount;
        public int MapId => _mapId;
        public IReadOnlyList<TileChunkRef> Chunks => _chunks;

        /// <summary>Substeps the latest tick ran.</summary>
        public int LastSubsteps { get; private set; }

        /// <summary>Indices of Active/Semiactive cells after the latest tick. Empty when disposed.</summary>
        public NativeArray<int> ActiveCells =>
            _activeCells.IsCreated ? _activeCells.AsArray() : default;

        public NativeArray<float>.ReadOnly MolesRead =>
            _molesRead.IsCreated ? _molesRead.AsReadOnly() : default;

        public NativeArray<AtmosCellMeta>.ReadOnly CellMeta =>
            _cellMeta.IsCreated ? _cellMeta.AsReadOnly() : default;

        public NativeArray<AtmosNeighbours>.ReadOnly Neighbours =>
            _neighbours.IsCreated ? _neighbours.AsReadOnly() : default;

        public NativeArray<float>.ReadOnly BurnIntensity =>
            _burnIntensity.IsCreated ? _burnIntensity.AsReadOnly() : default;

        public NativeArray<AtmosEdgeFlow>.ReadOnly EdgeFlow =>
            _edgeFlow.IsCreated ? _edgeFlow.AsReadOnly() : default;

        public AtmosSimulation(ITileQueryService query, int mapId, int gasTypeCount, float[] specificHeats = null, float[] molarMasses = null)
        {
            _query = query;
            _mapId = mapId;
            _gasTypeCount = Mathf.Clamp(gasTypeCount, 1, AtmosConstants.MaxGasTypes);
            _activeCells = new NativeList<int>(Allocator.Persistent);
            _workingSet = new NativeList<int>(Allocator.Persistent);
            _specificHeats = BuildSpecificHeats(specificHeats);
            _molarMasses = BuildMolarMasses(molarMasses);
        }

        private static NativeArray<float> BuildSpecificHeats(float[] overrides)
        {
            var heats = new NativeArray<float>(AtmosConstants.MaxGasTypes, Allocator.Persistent);

            // Core gas defaults first, so an unassigned registry still has sane thermodynamics.
            foreach (GasDefault gas in GasDefaults.Core)
            {
                if (gas.Id < AtmosConstants.MaxGasTypes)
                    heats[gas.Id] = gas.SpecificHeat;
            }

            if (overrides != null)
            {
                int count = Mathf.Min(overrides.Length, AtmosConstants.MaxGasTypes);
                for (int i = 0; i < count; i++)
                {
                    if (overrides[i] > 0f)
                        heats[i] = overrides[i];
                }
            }

            return heats;
        }

        private static NativeArray<float> BuildMolarMasses(float[] overrides)
        {
            var masses = new NativeArray<float>(AtmosConstants.MaxGasTypes, Allocator.Persistent);

            foreach (GasDefault gas in GasDefaults.Core)
            {
                if (gas.Id < AtmosConstants.MaxGasTypes)
                    masses[gas.Id] = gas.MolarMass;
            }

            if (overrides != null)
            {
                int count = Mathf.Min(overrides.Length, AtmosConstants.MaxGasTypes);
                for (int i = 0; i < count; i++)
                {
                    if (overrides[i] > 0f)
                        masses[i] = overrides[i];
                }
            }

            return masses;
        }

        public bool TryGetCellIndex(TileCoord coord, out int cellIndex)
        {
            return _coordToIndex.TryGetValue(coord, out cellIndex);
        }

        public bool TryGetChunkIndex(Vector2Int chunkKey, out int chunkIndex)
        {
            return _chunkKeyToIndex.TryGetValue(chunkKey, out chunkIndex);
        }

        public bool TryGetCellTemperature(TileCoord coord, out float temperature)
        {
            temperature = 0f;
            if (!_coordToIndex.TryGetValue(coord, out int cellIndex))
                return false;

            temperature = _cellMeta[cellIndex].Temperature;
            return true;
        }

        public float GetCellPressure(TileCoord coord)
        {
            if (!_coordToIndex.TryGetValue(coord, out int cellIndex))
                return 0f;

            return GetPressure(cellIndex, _cellMeta[cellIndex]);
        }

        public bool TryRemoveMoles(TileCoord coord, GasId gasId, float requestedMoles, out float removed, out float sourceTemperature)
        {
            removed = 0f;
            sourceTemperature = AtmosConstants.StandardTemperature;

            if (!_coordToIndex.TryGetValue(coord, out int cellIndex))
                return false;

            int moleIndex = GasMixture.GetMoleIndex(cellIndex, gasId);
            float available = _molesRead[moleIndex];
            if (available <= 0f)
                return false;

            removed = Mathf.Min(requestedMoles, available);
            _molesRead[moleIndex] -= removed;
            _molesWrite[moleIndex] -= removed;
            sourceTemperature = _cellMeta[cellIndex].Temperature;
            ActivateRegion(coord, 0);
            return removed > 0f;
        }

        public bool TryAddMolesAtTemperature(TileCoord coord, GasId gasId, float moles, float temperature)
        {
            if (moles <= 0f || !_coordToIndex.TryGetValue(coord, out int cellIndex))
                return false;

            int moleIndex = GasMixture.GetMoleIndex(cellIndex, gasId);
            float specificHeat = gasId.Value < _specificHeats.Length ? _specificHeats[gasId.Value] : 1f;
            if (specificHeat <= 0f)
                specificHeat = 1f;

            float currentCapacity = 0f;
            int baseIndex = cellIndex * AtmosConstants.MaxGasTypes;
            for (int id = 0; id < _gasTypeCount; id++)
                currentCapacity += _molesRead[baseIndex + id] * _specificHeats[id];

            AtmosCellMeta meta = _cellMeta[cellIndex];
            meta.Temperature = AtmosPipeThermo.BlendTemperature(
                meta.Temperature,
                currentCapacity,
                moles,
                specificHeat,
                temperature);

            _molesRead[moleIndex] += moles;
            _molesWrite[moleIndex] += moles;
            PromoteCellForGas(ref meta);
            _cellMeta[cellIndex] = meta;
            _cellMetaWrite[cellIndex] = meta;
            ActivateRegion(coord, 0);
            return true;
        }

        private static void PromoteCellForGas(ref AtmosCellMeta meta)
        {
            if (meta.State is AtmosCellState.Blocked)
                return;

            if (meta.State is AtmosCellState.Vacuum
                or AtmosCellState.Inactive
                or AtmosCellState.Semiactive)
            {
                meta.State = AtmosCellState.Active;
            }
        }

        public bool TryGetGasMoles(TileCoord coord, GasId gasId, out float moles)
        {
            moles = 0f;
            if (!_coordToIndex.TryGetValue(coord, out int cellIndex))
                return false;

            moles = _molesRead[GasMixture.GetMoleIndex(cellIndex, gasId)];
            return true;
        }

        public float GetMolarMass(GasId gasId)
        {
            if (!_molarMasses.IsCreated || gasId.Value >= _molarMasses.Length)
                return 1f;

            float mass = _molarMasses[gasId.Value];
            return mass > 0f ? mass : 1f;
        }

        public float GetCellPressure(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= _cellCount)
                return 0f;

            return GetPressure(cellIndex, _cellMeta[cellIndex]);
        }

        public float GetCellMoles(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= _cellCount)
                return 0f;

            float totalMoles = 0f;
            int baseIndex = cellIndex * AtmosConstants.MaxGasTypes;
            for (int gasId = 0; gasId < _gasTypeCount; gasId++)
                totalMoles += _molesRead[baseIndex + gasId];

            return totalMoles;
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
            RebuildBordersAround(chunkRef);
        }

        public void UpdateCell(TileCoord coord)
        {
            if (!_coordToIndex.TryGetValue(coord, out int cellIndex))
                return;

            InitCell(cellIndex, coord);
            ClearEdgeFlows(cellIndex, coord);
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
                    _cellMetaWrite[cellIndex] = meta;
                    QueueActivate(cellIndex);
                }
            }
        }

        /// <summary>
        /// Queues a cell to be re-evaluated at the start of the next tick. Used when a tile
        /// mutation is observed before the tilemap has finished applying it (e.g. a clear notifies
        /// before the occupant is removed), so re-reading occupancy immediately would be stale.
        /// </summary>
        public void QueueCellRefresh(TileCoord coord)
        {
            _pendingRefresh.Add(coord);
        }

        public void Tick(float deltaTime)
        {
            FlushPendingRefresh();

            if (_cellCount == 0)
                return;

            RebuildActiveList();
            if (_activeCells.Length == 0)
                return;

            BuildWorkingSet();
            ClearBurnIntensityActiveOnly();

            int substeps = GetEdgeFlowSubsteps();
            LastSubsteps = substeps;
            float subDelta = deltaTime / substeps;

            // Only edge flow has a wave to keep stable. Everything else runs once, as on a calm tick.
            for (int step = 0; step < substeps; step++)
                RunShareGasJob(subDelta, step == substeps - 1 ? deltaTime : 0f);

            RunReactJob(deltaTime);
            RunConductHeatJob(deltaTime);

            RefreshActiveListFromWorkingSet();
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
                BurnIntensity = _burnIntensity.IsCreated ? _burnIntensity[cellIndex] : 0f,
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

        /// <summary>
        /// Total thermal energy across all cells (Σ heatCapacity·T). Conserved by advection and
        /// conduction in a sealed room; used as a test invariant.
        /// </summary>
        public float GetTotalThermalEnergy()
        {
            float total = 0f;
            for (int cellIndex = 0; cellIndex < _cellCount; cellIndex++)
            {
                float heatCapacity = 0f;
                int baseIndex = cellIndex * AtmosConstants.MaxGasTypes;
                for (int gasId = 0; gasId < _gasTypeCount; gasId++)
                    heatCapacity += _molesRead[baseIndex + gasId] * _specificHeats[gasId];

                total += heatCapacity * _cellMeta[cellIndex].Temperature;
            }

            return total;
        }

        public void ForEachCoord(System.Action<TileCoord> visitor)
        {
            foreach (TileCoord coord in _coordToIndex.Keys)
                visitor(coord);
        }

        public void ForEachCell(System.Action<TileCoord, int> visitor)
        {
            foreach (KeyValuePair<TileCoord, int> pair in _coordToIndex)
                visitor(pair.Key, pair.Value);
        }

        public void DebugAddMoles(TileCoord coord, GasId gasId, float moles)
        {
            if (!_coordToIndex.TryGetValue(coord, out int cellIndex))
                return;

            int moleIndex = GasMixture.GetMoleIndex(cellIndex, gasId);
            _molesRead[moleIndex] += moles;
            _molesWrite[moleIndex] += moles;
            AtmosCellMeta meta = _cellMeta[cellIndex];
            PromoteCellForGas(ref meta);
            _cellMeta[cellIndex] = meta;
            _cellMetaWrite[cellIndex] = meta;
            ActivateRegion(coord, 0);
        }

        public float DebugGetMoles(TileCoord coord, GasId gasId)
        {
            if (!_coordToIndex.TryGetValue(coord, out int cellIndex))
                return 0f;

            return _molesRead[GasMixture.GetMoleIndex(cellIndex, gasId)];
        }

        public void DebugSetTemperature(TileCoord coord, float temperature)
        {
            if (!_coordToIndex.TryGetValue(coord, out int cellIndex))
                return;

            AtmosCellMeta meta = _cellMeta[cellIndex];
            meta.Temperature = temperature;
            _cellMeta[cellIndex] = meta;
            _cellMetaWrite[cellIndex] = meta;
            ActivateRegion(coord, 0);
        }

        public void DebugAddHeat(TileCoord coord, float deltaKelvin)
        {
            if (!_coordToIndex.TryGetValue(coord, out int cellIndex))
                return;

            AtmosCellMeta meta = _cellMeta[cellIndex];
            meta.Temperature = Mathf.Max(0f, meta.Temperature + deltaKelvin);
            _cellMeta[cellIndex] = meta;
            _cellMetaWrite[cellIndex] = meta;
            ActivateRegion(coord, 0);
        }

        public void Dispose()
        {
            if (_molesRead.IsCreated) _molesRead.Dispose();
            if (_molesWrite.IsCreated) _molesWrite.Dispose();
            if (_cellMeta.IsCreated) _cellMeta.Dispose();
            if (_cellMetaWrite.IsCreated) _cellMetaWrite.Dispose();
            if (_neighbours.IsCreated) _neighbours.Dispose();
            if (_energyScratch.IsCreated) _energyScratch.Dispose();
            if (_burnIntensity.IsCreated) _burnIntensity.Dispose();
            if (_edgeFlow.IsCreated) _edgeFlow.Dispose();
            if (_edgeScratch.IsCreated) _edgeScratch.Dispose();
            if (_specificHeats.IsCreated) _specificHeats.Dispose();
            if (_molarMasses.IsCreated) _molarMasses.Dispose();
            if (_activeCells.IsCreated) _activeCells.Dispose();
            if (_workingSet.IsCreated) _workingSet.Dispose();
            _pendingActivate.Clear();
            _setScratch.Clear();
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

            // Airtight walls block flow entirely, independent of whatever turf sits beneath them.
            if (occupancy.HasWall && occupancy.IsAirtight)
            {
                meta.State = AtmosCellState.Blocked;
                _cellMeta[cellIndex] = meta;
                _cellMetaWrite[cellIndex] = meta;
                return;
            }

            if (!occupancy.HasPlenum)
            {
                meta.Temperature = AtmosConstants.SpaceTemperature;
                meta.State = AtmosCellState.Vacuum;
                _cellMeta[cellIndex] = meta;
                _cellMetaWrite[cellIndex] = meta;
                return;
            }

            SetMoles(cellIndex, AtmosConstants.Oxygen, AtmosConstants.StationOxygenMoles);
            SetMoles(cellIndex, AtmosConstants.Nitrogen, AtmosConstants.StationNitrogenMoles);
            meta.State = AtmosCellState.Active;

            _cellMeta[cellIndex] = meta;
            _cellMetaWrite[cellIndex] = meta;
            QueueActivate(cellIndex);
        }

        // A changed cell starts from rest: a door that opens later must not resume the old flow.
        private void ClearEdgeFlows(int cellIndex, TileCoord coord)
        {
            _edgeFlow[cellIndex] = default;

            if (_coordToIndex.TryGetValue(AtmosNeighbourBuilder.GetNeighbourCoord(coord, Direction.West), out int westIndex))
            {
                AtmosEdgeFlow west = _edgeFlow[westIndex];
                west.East = 0f;
                _edgeFlow[westIndex] = west;
            }

            if (_coordToIndex.TryGetValue(AtmosNeighbourBuilder.GetNeighbourCoord(coord, Direction.South), out int southIndex))
            {
                AtmosEdgeFlow south = _edgeFlow[southIndex];
                south.North = 0f;
                _edgeFlow[southIndex] = south;
            }
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

        /// <summary>
        /// Links cells of already-created chunks back into a new chunk. Each cell only pushes gas
        /// through its own links, so a missing return link makes the chunk border a one-way valve.
        /// </summary>
        private void RebuildBordersAround(TileChunkRef chunkRef)
        {
            int minX = chunkRef.ChunkKey.x * AtmosConstants.ChunkSize;
            int minY = chunkRef.ChunkKey.y * AtmosConstants.ChunkSize;
            int maxX = minX + AtmosConstants.ChunkSize - 1;
            int maxY = minY + AtmosConstants.ChunkSize - 1;

            for (int offset = 0; offset < AtmosConstants.ChunkSize; offset++)
            {
                RebuildNeighboursForCoord(new TileCoord(chunkRef.MapId, minX - 1, minY + offset));
                RebuildNeighboursForCoord(new TileCoord(chunkRef.MapId, maxX + 1, minY + offset));
                RebuildNeighboursForCoord(new TileCoord(chunkRef.MapId, minX + offset, minY - 1));
                RebuildNeighboursForCoord(new TileCoord(chunkRef.MapId, minX + offset, maxY + 1));
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

        private void FlushPendingRefresh()
        {
            if (_pendingRefresh.Count == 0)
                return;

            foreach (TileCoord coord in _pendingRefresh)
            {
                UpdateCell(coord);
                ActivateRegion(coord, 2);
            }

            _pendingRefresh.Clear();
        }

        private void RebuildActiveList()
        {
            _setScratch.Clear();
            for (int i = 0; i < _activeCells.Length; i++)
                _setScratch.Add(_activeCells[i]);

            foreach (int cellIndex in _pendingActivate)
                _setScratch.Add(cellIndex);

            _pendingActivate.Clear();
            _activeCells.Clear();

            foreach (int cellIndex in _setScratch)
            {
                if (cellIndex < 0 || cellIndex >= _cellCount)
                    continue;

                if (_cellMeta[cellIndex].IsSimulated)
                    _activeCells.Add(cellIndex);
            }
        }

        private void BuildWorkingSet()
        {
            _setScratch.Clear();
            _workingSet.Clear();

            for (int i = 0; i < _activeCells.Length; i++)
            {
                int cellIndex = _activeCells[i];
                if (_setScratch.Add(cellIndex))
                    _workingSet.Add(cellIndex);

                for (int direction = 0; direction < 4; direction++)
                {
                    int neighbourIndex = _neighbours[cellIndex].Get(direction);
                    if (neighbourIndex < 0 || neighbourIndex >= _cellCount)
                        continue;

                    if (_setScratch.Add(neighbourIndex))
                        _workingSet.Add(neighbourIndex);
                }
            }
        }

        private void RefreshActiveListFromWorkingSet()
        {
            _activeCells.Clear();
            for (int i = 0; i < _workingSet.Length; i++)
            {
                int cellIndex = _workingSet[i];
                if (cellIndex < 0 || cellIndex >= _cellCount)
                    continue;

                if (_cellMeta[cellIndex].IsSimulated)
                    _activeCells.Add(cellIndex);
                else if (_burnIntensity.IsCreated)
                    _burnIntensity[cellIndex] = 0f;
            }
        }

        private void QueueActivate(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= _cellCount)
                return;

            _pendingActivate.Add(cellIndex);
        }

        // Fixed count while anything is moving, so wave speed stays the same through a breach; a
        // calm station keeps the cost of a single substep.
        private int GetEdgeFlowSubsteps()
        {
            for (int i = 0; i < _activeCells.Length; i++)
            {
                int cellIndex = _activeCells[i];
                float selfPressure = GetPressure(cellIndex, _cellMeta[cellIndex]);
                AtmosEdgeFlow selfFlow = _edgeFlow[cellIndex];
                if (Mathf.Abs(selfFlow.North) > AtmosFluxConstants.EdgeFlowSubstepFlow ||
                    Mathf.Abs(selfFlow.East) > AtmosFluxConstants.EdgeFlowSubstepFlow)
                {
                    return AtmosFluxConstants.MaxBreachSubsteps;
                }

                for (int direction = 0; direction < 4; direction++)
                {
                    int neighbourIndex = _neighbours[cellIndex].Get(direction);
                    if (neighbourIndex < 0 || _cellMeta[neighbourIndex].State == AtmosCellState.Blocked)
                        continue;

                    float neighbourPressure = GetPressure(neighbourIndex, _cellMeta[neighbourIndex]);
                    if (Mathf.Abs(selfPressure - neighbourPressure) > AtmosFluxConstants.EdgeFlowSubstepPressure)
                        return AtmosFluxConstants.MaxBreachSubsteps;

                    if (direction >= 2 &&
                        Mathf.Abs(_edgeFlow[neighbourIndex].Get(direction - 2)) > AtmosFluxConstants.EdgeFlowSubstepFlow)
                    {
                        return AtmosFluxConstants.MaxBreachSubsteps;
                    }
                }
            }

            return 1;
        }

        private void RunShareGasJob(float deltaTime, float cellPassDeltaTime)
        {
            var job = new ShareGasJob
            {
                ActiveCells = _activeCells.AsArray(),
                WorkingSet = _workingSet.AsArray(),
                MolesRead = _molesRead,
                MolesWrite = _molesWrite,
                CellMeta = _cellMeta,
                CellMetaWrite = _cellMetaWrite,
                Neighbours = _neighbours,
                SpecificHeat = _specificHeats,
                EnergyScratch = _energyScratch,
                BurnIntensity = _burnIntensity,
                EdgeFlow = _edgeFlow,
                EdgeScratch = _edgeScratch,
                MaxGasTypes = AtmosConstants.MaxGasTypes,
                GasTypeCount = _gasTypeCount,
                DeltaTime = deltaTime,
                CellPassDeltaTime = cellPassDeltaTime,
                SpaceTemperature = AtmosConstants.SpaceTemperature,
            };

            job.Schedule().Complete();
        }

        private void RunReactJob(float deltaTime)
        {
            var job = new ReactAtmosJob
            {
                ActiveCells = _activeCells.AsArray(),
                SpecificHeat = _specificHeats,
                Moles = _molesRead,
                CellMeta = _cellMeta,
                BurnIntensity = _burnIntensity,
                MaxGasTypes = AtmosConstants.MaxGasTypes,
                GasTypeCount = _gasTypeCount,
                OxygenId = AtmosConstants.Oxygen.Value,
                PlasmaId = AtmosConstants.Plasma.Value,
                CarbonDioxideId = AtmosConstants.CarbonDioxide.Value,
                DeltaTime = deltaTime,
            };

            // Reactions are local (single cell), so they run in place without buffer swaps.
            job.Schedule().Complete();
        }

        private void RunConductHeatJob(float deltaTime)
        {
            var job = new ConductHeatJob
            {
                ActiveCells = _activeCells.AsArray(),
                WorkingSet = _workingSet.AsArray(),
                Moles = _molesRead,
                CellMeta = _cellMeta,
                Neighbours = _neighbours,
                SpecificHeat = _specificHeats,
                CellMetaWrite = _cellMetaWrite,
                EnergyScratch = _energyScratch,
                MaxGasTypes = AtmosConstants.MaxGasTypes,
                GasTypeCount = _gasTypeCount,
                DeltaTime = deltaTime,
                SpaceTemperature = AtmosConstants.SpaceTemperature,
            };

            job.Schedule().Complete();
        }

        private void ClearBurnIntensityActiveOnly()
        {
            if (!_burnIntensity.IsCreated)
                return;

            for (int i = 0; i < _activeCells.Length; i++)
            {
                int cellIndex = _activeCells[i];
                if (cellIndex >= 0 && cellIndex < _burnIntensity.Length)
                    _burnIntensity[cellIndex] = 0f;
            }
        }

        private void ResizeBuffers(int cellCount)
        {
            int moleStride = cellCount * AtmosConstants.MaxGasTypes;
            ResizeNativeArray(ref _molesRead, moleStride);
            ResizeNativeArray(ref _molesWrite, moleStride);
            ResizeNativeArray(ref _cellMeta, cellCount);
            ResizeNativeArray(ref _cellMetaWrite, cellCount);
            ResizeNativeArray(ref _neighbours, cellCount);
            ResizeNativeArray(ref _energyScratch, cellCount);
            ResizeNativeArray(ref _burnIntensity, cellCount);
            ResizeNativeArray(ref _edgeFlow, cellCount);
            ResizeNativeArray(ref _edgeScratch, cellCount);
        }

        private static void ResizeNativeArray<T>(ref NativeArray<T> array, int length) where T : struct
        {
            if (length <= 0)
            {
                if (array.IsCreated)
                    array.Dispose();
                array = default;
                return;
            }

            var resized = new NativeArray<T>(length, Allocator.Persistent);

            if (array.IsCreated)
            {
                // Preserve already-initialised cells; growing must not wipe existing chunks.
                int copyLength = Mathf.Min(array.Length, length);
                NativeArray<T>.Copy(array, resized, copyLength);
                array.Dispose();
            }

            array = resized;
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

            return GetCellMoles(cellIndex) * AtmosFluxConstants.GasConstant * meta.Temperature / meta.Volume / 1000f;
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
