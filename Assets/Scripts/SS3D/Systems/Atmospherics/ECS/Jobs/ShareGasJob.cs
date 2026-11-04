using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SS3D.Systems.Atmospherics.ECS
{
    /// <summary>
    /// Bulk transport along per-edge flow with momentum, plus equal-pressure composition diffusion
    /// over the active cell list. Only the <see cref="WorkingSet"/> (actives + their neighbours) is
    /// copied, seeded, and temperature-resolved — passive cells stay untouched.
    /// Commits results back into the read buffers for the working set (no full-grid swap).
    /// </summary>
    [BurstCompile]
    public struct ShareGasJob : IJob
    {
        [ReadOnly] public NativeArray<int> ActiveCells;
        [ReadOnly] public NativeArray<int> WorkingSet;
        [ReadOnly] public NativeArray<AtmosNeighbours> Neighbours;
        [ReadOnly] public NativeArray<float> SpecificHeat;

        public NativeArray<float> MolesRead;
        public NativeArray<float> MolesWrite;
        public NativeArray<AtmosCellMeta> CellMeta;
        public NativeArray<AtmosCellMeta> CellMetaWrite;

        /// <summary>Per-cell thermal energy scratch (heatCapacity·T), seeded each run.</summary>
        public NativeArray<float> EnergyScratch;

        public NativeArray<float> BurnIntensity;

        /// <summary>Stored east and north flow per cell; see <see cref="AtmosEdgeFlow"/>.</summary>
        public NativeArray<AtmosEdgeFlow> EdgeFlow;

        public NativeArray<AtmosEdgeCellScratch> EdgeScratch;

        public int MaxGasTypes;
        public int GasTypeCount;
        public float DeltaTime;
        public float SpaceTemperature;

        /// <summary>
        /// Time since the cell pass last ran, or zero to skip it. Only edge flow needs substeps, so
        /// diffusion, settling and sleep run once a tick, on the last substep.
        /// </summary>
        public float CellPassDeltaTime;

        public void Execute()
        {
            for (int w = 0; w < WorkingSet.Length; w++)
            {
                int c = WorkingSet[w];
                if (c < 0 || c >= CellMeta.Length)
                    continue;

                int baseIndex = c * MaxGasTypes;
                for (int gasId = 0; gasId < MaxGasTypes; gasId++)
                    MolesWrite[baseIndex + gasId] = MolesRead[baseIndex + gasId];

                CellMetaWrite[c] = CellMeta[c];

                float heatCapacity = AtmosThermo.HeatCapacity(MolesRead, SpecificHeat, c, MaxGasTypes, GasTypeCount);
                EnergyScratch[c] = heatCapacity * CellMeta[c].Temperature;
            }

            MoveBulkGasAlongEdges();

            int cellPassCount = CellPassDeltaTime > 0f ? ActiveCells.Length : 0;
            for (int activeIndex = 0; activeIndex < cellPassCount; activeIndex++)
            {
                int cellIndex = ActiveCells[activeIndex];
                AtmosCellMeta self = CellMeta[cellIndex];

                if (!self.IsSimulated)
                    continue;

                float selfPressure = GetPressure(cellIndex, self.Temperature, self.Volume);
                bool transferred = false;

                for (int direction = 0; direction < 4; direction++)
                {
                    int neighbourIndex = Neighbours[cellIndex].Get(direction);
                    if (neighbourIndex < 0 || neighbourIndex >= CellMeta.Length)
                        continue;

                    AtmosCellMeta neighbour = CellMeta[neighbourIndex];
                    if (neighbour.State == AtmosCellState.Blocked)
                        continue;

                    float neighbourPressure = GetPressure(neighbourIndex, neighbour.Temperature, neighbour.Volume);
                    float pressureDiff = selfPressure - neighbourPressure;

                    if (IsEdgeUnsettled(cellIndex, direction, neighbourIndex, pressureDiff))
                        transferred = true;

                    // Equal-pressure composition diffusion: total P matched, but mole fractions may
                    // still differ (breath O₂→CO₂ pockets). Thin gas stops diffusing: nobody breathes
                    // it, and it would otherwise creep toward a breach for minutes, holding cells
                    // awake and dirty.
                    if (neighbour.State != AtmosCellState.Vacuum &&
                        selfPressure >= AtmosFluxConstants.MinSimulationPressure &&
                        math.abs(pressureDiff) <= AtmosFluxConstants.PressureEpsilon &&
                        DiffuseAlongPartialPressures(cellIndex, neighbourIndex, self, neighbour))
                    {
                        transferred = true;
                    }
                }

                // Low-pressure settle: once a cell is nearly evacuated and still has somewhere to
                // drain (vacuum, or a neighbour that is itself this empty), dump its last traces and
                // sleep. Venting is proportional to the pressure gap, so without this a breached room
                // never actually reaches zero — it creeps down an ever-slower exponential while the
                // cells stay awake. The lost gas has effectively already vented to space.
                if (selfPressure < AtmosFluxConstants.MinSimulationPressure && HasDrainSink(cellIndex))
                {
                    for (int gasId = 0; gasId < GasTypeCount; gasId++)
                        MolesWrite[GetMoleIndex(cellIndex, gasId)] = 0f;

                    EnergyScratch[cellIndex] = 0f;
                    ClearEdges(cellIndex);

                    AtmosCellMeta settled = CellMetaWrite[cellIndex];
                    settled.State = AtmosCellState.Inactive;
                    CellMetaWrite[cellIndex] = settled;
                    ClearBurn(cellIndex);
                    continue;
                }

                // If we moved gas this tick our pressure changed, so every dormant neighbour
                // should re-check next tick. This lets the active front advance one tile per
                // tick instead of waiting for the pressure gap to slowly build past epsilon.
                if (transferred)
                {
                    for (int direction = 0; direction < 4; direction++)
                    {
                        int neighbourIndex = Neighbours[cellIndex].Get(direction);
                        if (neighbourIndex < 0 || neighbourIndex >= CellMeta.Length)
                            continue;

                        WakeNeighbour(neighbourIndex, CellMeta[neighbourIndex]);
                    }
                }

                AtmosCellMeta selfWrite = CellMetaWrite[cellIndex];

                // A neighbour that pushed gas into us this tick already flipped our write state to Active.
                bool reactivatedByInflow = selfWrite.State == AtmosCellState.Active
                    && self.State != AtmosCellState.Active;

                if (transferred || reactivatedByInflow)
                {
                    // Gas moved in or out: keep the cell fully awake.
                    selfWrite.State = AtmosCellState.Active;
                }
                else if (self.State == AtmosCellState.Active)
                {
                    // Was active but nothing moved: cool down to the grace state.
                    selfWrite.State = AtmosCellState.Semiactive;
                }
                else if (self.State == AtmosCellState.Semiactive)
                {
                    // Still nothing moved after the grace tick: settle and drop out of the sim.
                    selfWrite.State = AtmosCellState.Inactive;
                    ClearBurn(cellIndex);
                }

                // Any other state (Inactive / Vacuum / Blocked) is left untouched.
                CellMetaWrite[cellIndex] = selfWrite;
            }

            // Derive temperatures only for the working set — passive cells keep last committed T.
            for (int w = 0; w < WorkingSet.Length; w++)
            {
                int c = WorkingSet[w];
                if (c < 0 || c >= CellMetaWrite.Length)
                    continue;

                AtmosCellMeta meta = CellMetaWrite[c];
                if (meta.State == AtmosCellState.Blocked || meta.State == AtmosCellState.Vacuum)
                    continue;

                float heatCapacity = AtmosThermo.HeatCapacity(MolesWrite, SpecificHeat, c, MaxGasTypes, GasTypeCount);
                float totalMoles = AtmosThermo.TotalMoles(MolesWrite, c, MaxGasTypes, GasTypeCount);
                meta.Temperature = AtmosThermo.ResolveTemperature(EnergyScratch[c], heatCapacity, totalMoles, SpaceTemperature);
                CellMetaWrite[c] = meta;
            }

            // Commit working-set results into the authoritative read buffers.
            for (int w = 0; w < WorkingSet.Length; w++)
            {
                int c = WorkingSet[w];
                if (c < 0 || c >= CellMeta.Length)
                    continue;

                int baseIndex = c * MaxGasTypes;
                for (int gasId = 0; gasId < MaxGasTypes; gasId++)
                    MolesRead[baseIndex + gasId] = MolesWrite[baseIndex + gasId];

                CellMeta[c] = CellMetaWrite[c];
            }

            for (int activeIndex = 0; activeIndex < ActiveCells.Length; activeIndex++)
            {
                int c = ActiveCells[activeIndex];
                AtmosEdgeCellScratch scratch = EdgeScratch[c];
                scratch.Awake = false;
                EdgeScratch[c] = scratch;
            }
        }

        // One pass per open edge, from its owning cell, so each move writes both ends and moles
        // are conserved by construction. Pressures and compositions come from the read buffer, so
        // the result does not depend on cell order.
        private void MoveBulkGasAlongEdges()
        {
            SeedEdgeScratch();
            AdvanceEdgeFlows();
            ScaleOutflows();

            for (int w = 0; w < WorkingSet.Length; w++)
            {
                int c = WorkingSet[w];
                if (c < 0 || c >= CellMeta.Length)
                    continue;

                for (int axis = 0; axis < 2; axis++)
                {
                    if (!TryGetLiveEdge(c, axis, out int neighbourIndex))
                        continue;

                    AtmosEdgeFlow edges = EdgeFlow[c];
                    float flow = edges.Get(axis);
                    if (flow == 0f)
                        continue;

                    int upwind = flow > 0f ? c : neighbourIndex;
                    int downwind = flow > 0f ? neighbourIndex : c;
                    flow *= EdgeScratch[upwind].OutflowScale;
                    edges.Set(axis, flow);
                    EdgeFlow[c] = edges;
                    MoveMoles(upwind, downwind, math.abs(flow) * DeltaTime);
                }
            }
        }

        private void SeedEdgeScratch()
        {
            for (int w = 0; w < WorkingSet.Length; w++)
            {
                int c = WorkingSet[w];
                if (c < 0 || c >= CellMeta.Length)
                    continue;

                AtmosCellMeta meta = CellMeta[c];
                EdgeScratch[c] = new AtmosEdgeCellScratch
                {
                    Pressure = GetPressure(c, meta.Temperature, meta.Volume),
                    TotalMoles = AtmosThermo.TotalMoles(MolesRead, c, MaxGasTypes, GasTypeCount),
                    OutflowScale = 1f,
                };
            }

            // CellMeta can wake a cell mid-tick whose own neighbours are outside the working set,
            // so only the tick's active list may open an edge: writes outside the set are lost.
            for (int activeIndex = 0; activeIndex < ActiveCells.Length; activeIndex++)
            {
                int c = ActiveCells[activeIndex];
                AtmosEdgeCellScratch scratch = EdgeScratch[c];
                scratch.Awake = true;
                EdgeScratch[c] = scratch;
            }
        }

        private void AdvanceEdgeFlows()
        {
            float maxWaveSpeedSquared = AtmosFluxConstants.EdgeFlowStabilityLimit / (DeltaTime * DeltaTime);
            float waveSpeedSquared = AtmosFluxConstants.EdgeFlowWaveSpeed * AtmosFluxConstants.EdgeFlowWaveSpeed;
            float friction = 1f / (1f + (AtmosFluxConstants.EdgeFlowFriction * DeltaTime));

            for (int w = 0; w < WorkingSet.Length; w++)
            {
                int c = WorkingSet[w];
                if (c < 0 || c >= CellMeta.Length)
                    continue;

                for (int axis = 0; axis < 2; axis++)
                {
                    if (!TryGetLiveEdge(c, axis, out int neighbourIndex))
                        continue;

                    AtmosCellMeta metaA = CellMeta[c];
                    AtmosCellMeta metaB = CellMeta[neighbourIndex];
                    AtmosEdgeCellScratch scratchA = EdgeScratch[c];
                    AtmosEdgeCellScratch scratchB = EdgeScratch[neighbourIndex];

                    AtmosEdgeFlow edges = EdgeFlow[c];
                    float flow = edges.Get(axis);
                    float pressureDiff = scratchA.Pressure - scratchB.Pressure;

                    if (math.abs(pressureDiff) <= AtmosFluxConstants.EdgeFlowSettledPressure &&
                        math.abs(flow) < AtmosFluxConstants.EdgeFlowSettledFlow)
                    {
                        edges.Set(axis, 0f);
                        EdgeFlow[c] = edges;
                        continue;
                    }

                    // Local wave speed follows temperature, so a fire cannot outrun the time step.
                    float heat = math.max(metaA.Temperature, metaB.Temperature) / AtmosFluxConstants.EdgeFlowReferenceTemperature;
                    float localSpeedSquared = math.max(waveSpeedSquared * heat, waveSpeedSquared);
                    float stiffness = math.min(1f, maxWaveSpeedSquared / localSpeedSquared);
                    float molesPerKilopascal = 1000f * metaA.Volume /
                        (AtmosFluxConstants.GasConstant * AtmosFluxConstants.EdgeFlowReferenceTemperature);

                    flow += DeltaTime * stiffness * waveSpeedSquared * molesPerKilopascal * pressureDiff;
                    flow *= friction;

                    int upwind = flow > 0f ? c : neighbourIndex;
                    bool vacuumEdge = metaA.State == AtmosCellState.Vacuum || metaB.State == AtmosCellState.Vacuum;
                    float maxSpeed = vacuumEdge ? AtmosFluxConstants.EdgeFlowVacuumMaxSpeed : AtmosFluxConstants.EdgeFlowMaxSpeed;
                    float maxFlow = maxSpeed * EdgeScratch[upwind].TotalMoles;
                    flow = math.clamp(flow, -maxFlow, maxFlow);

                    edges.Set(axis, flow);
                    EdgeFlow[c] = edges;

                    AtmosEdgeCellScratch upwindScratch = EdgeScratch[upwind];
                    upwindScratch.Outflow += math.abs(flow);
                    EdgeScratch[upwind] = upwindScratch;
                }
            }
        }

        private void ScaleOutflows()
        {
            for (int w = 0; w < WorkingSet.Length; w++)
            {
                int c = WorkingSet[w];
                if (c < 0 || c >= CellMeta.Length)
                    continue;

                AtmosEdgeCellScratch scratch = EdgeScratch[c];
                float leaving = scratch.Outflow * DeltaTime;
                float allowed = scratch.TotalMoles * AtmosFluxConstants.EdgeFlowMaxOutflowFraction;
                if (leaving > allowed)
                {
                    scratch.OutflowScale = allowed / leaving;
                    EdgeScratch[c] = scratch;
                }
            }
        }

        // The mixture moves as a whole at the upwind temperature, so bulk flow never separates
        // gases. Vacuum takes what it is given and keeps none of it.
        private void MoveMoles(int upwind, int downwind, float moles)
        {
            float total = EdgeScratch[upwind].TotalMoles;
            if (moles <= 0f || total <= 0f)
                return;

            float temperature = CellMeta[upwind].Temperature;
            bool intoVacuum = CellMeta[downwind].State == AtmosCellState.Vacuum;
            float share = moles / total;

            for (int gasId = 0; gasId < GasTypeCount; gasId++)
            {
                int upwindIndex = GetMoleIndex(upwind, gasId);
                float moved = MolesRead[upwindIndex] * share;
                float energyMoved = moved * SpecificHeat[gasId] * temperature;

                MolesWrite[upwindIndex] -= moved;
                EnergyScratch[upwind] -= energyMoved;
                if (!intoVacuum)
                {
                    MolesWrite[GetMoleIndex(downwind, gasId)] += moved;
                    EnergyScratch[downwind] += energyMoved;
                }
            }

            SetActive(upwind);
            SetActive(downwind);
        }

        private void SetActive(int cellIndex)
        {
            AtmosCellMeta write = CellMetaWrite[cellIndex];
            if (write.State == AtmosCellState.Vacuum || write.State == AtmosCellState.Blocked)
                return;

            write.State = AtmosCellState.Active;
            CellMetaWrite[cellIndex] = write;
        }

        // Finds the neighbour across an edge owned by this cell, if the edge can carry flow this
        // substep. A wall or closed door has no flow to keep.
        private bool TryGetLiveEdge(int cellIndex, int axis, out int neighbourIndex)
        {
            neighbourIndex = Neighbours[cellIndex].Get(axis);
            if (neighbourIndex < 0 || neighbourIndex >= CellMeta.Length)
                return false;

            if (!EdgeScratch[cellIndex].Awake && !EdgeScratch[neighbourIndex].Awake)
                return false;

            if (CellMeta[cellIndex].State == AtmosCellState.Blocked || CellMeta[neighbourIndex].State == AtmosCellState.Blocked)
            {
                AtmosEdgeFlow edges = EdgeFlow[cellIndex];
                edges.Set(axis, 0f);
                EdgeFlow[cellIndex] = edges;
                return false;
            }

            return true;
        }

        // A cell stays awake until its pressure is settled and every edge has stopped flowing.
        private bool IsEdgeUnsettled(int cellIndex, int direction, int neighbourIndex, float pressureDiff)
        {
            float flow = direction <= AtmosEdgeFlow.EastAxis
                ? EdgeFlow[cellIndex].Get(direction)
                : EdgeFlow[neighbourIndex].Get(direction - 2);

            return math.abs(pressureDiff) > AtmosFluxConstants.EdgeFlowSettledPressure ||
                math.abs(flow) >= AtmosFluxConstants.EdgeFlowSettledFlow;
        }

        private void ClearEdges(int cellIndex)
        {
            EdgeFlow[cellIndex] = default;

            for (int direction = 2; direction < 4; direction++)
            {
                int neighbourIndex = Neighbours[cellIndex].Get(direction);
                if (neighbourIndex < 0 || neighbourIndex >= EdgeFlow.Length)
                    continue;

                AtmosEdgeFlow edges = EdgeFlow[neighbourIndex];
                edges.Set(direction - 2, 0f);
                EdgeFlow[neighbourIndex] = edges;
            }
        }

        private void ClearBurn(int cellIndex)
        {
            if (BurnIntensity.IsCreated && cellIndex >= 0 && cellIndex < BurnIntensity.Length)
                BurnIntensity[cellIndex] = 0f;
        }

        private bool DiffuseAlongPartialPressures(
            int cellIndex,
            int neighbourIndex,
            AtmosCellMeta self,
            AtmosCellMeta neighbour)
        {
            bool transferred = false;

            for (int gasId = 0; gasId < GasTypeCount; gasId++)
            {
                int selfMoleIndex = GetMoleIndex(cellIndex, gasId);
                int neighbourMoleIndex = GetMoleIndex(neighbourIndex, gasId);

                float selfPartial = GetPartialPressure(MolesRead[selfMoleIndex], self.Temperature, self.Volume);
                float neighbourPartial = GetPartialPressure(MolesRead[neighbourMoleIndex], neighbour.Temperature, neighbour.Volume);
                float partialDiff = selfPartial - neighbourPartial;
                if (partialDiff <= AtmosFluxConstants.PartialPressureEpsilon)
                    continue;

                float molesToTransfer = partialDiff * 1000f * self.Volume /
                    (self.Temperature * AtmosFluxConstants.GasConstant);
                molesToTransfer *= AtmosFluxConstants.DiffusionSpeed * CellPassDeltaTime;
                molesToTransfer = math.min(molesToTransfer, MolesWrite[selfMoleIndex]);
                if (molesToTransfer <= 0f)
                    continue;

                // Moles leave the source carrying their enthalpy at the source temperature.
                float energyMoved = molesToTransfer * SpecificHeat[gasId] * self.Temperature;

                MolesWrite[selfMoleIndex] -= molesToTransfer;
                EnergyScratch[cellIndex] -= energyMoved;
                MolesWrite[neighbourMoleIndex] += molesToTransfer;
                EnergyScratch[neighbourIndex] += energyMoved;

                transferred = true;
                SetActive(neighbourIndex);
            }

            return transferred;
        }

        // True if the cell borders open space where remaining gas can vent.
        private bool HasDrainSink(int cellIndex)
        {
            for (int direction = 0; direction < 4; direction++)
            {
                int neighbourIndex = Neighbours[cellIndex].Get(direction);
                if (neighbourIndex < 0 || neighbourIndex >= CellMeta.Length)
                    continue;

                if (CellMeta[neighbourIndex].State == AtmosCellState.Vacuum)
                    return true;
            }

            return false;
        }

        private void WakeNeighbour(int neighbourIndex, AtmosCellMeta neighbour)
        {
            if (neighbour.State == AtmosCellState.Vacuum || neighbour.State == AtmosCellState.Blocked)
                return;

            AtmosCellMeta write = CellMetaWrite[neighbourIndex];
            if (write.State == AtmosCellState.Inactive || write.State == AtmosCellState.Semiactive)
            {
                write.State = AtmosCellState.Active;
                CellMetaWrite[neighbourIndex] = write;
            }
        }

        private int GetMoleIndex(int cellIndex, int gasId) => cellIndex * MaxGasTypes + gasId;

        private float GetPartialPressure(float moles, float temperature, float volume)
        {
            if (volume <= 0f || temperature <= 0f)
                return 0f;

            return moles * AtmosFluxConstants.GasConstant * temperature / volume / 1000f;
        }

        private float GetPressure(int cellIndex, float temperature, float volume)
        {
            if (volume <= 0f || temperature <= 0f)
                return 0f;

            float totalMoles = 0f;
            for (int gasId = 0; gasId < GasTypeCount; gasId++)
                totalMoles += MolesRead[GetMoleIndex(cellIndex, gasId)];

            return totalMoles * AtmosFluxConstants.GasConstant * temperature / volume / 1000f;
        }
    }
}
