using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SS3D.Systems.Atmospherics.ECS
{
    /// <summary>
    /// Pressure-driven mole sharing over the active cell list. Reads molesRead, writes molesWrite.
    /// </summary>
    [BurstCompile]
    public struct ShareGasJob : IJob
    {
        [ReadOnly] public NativeArray<int> ActiveCells;
        [ReadOnly] public NativeArray<float> MolesRead;
        [ReadOnly] public NativeArray<AtmosCellMeta> CellMeta;
        [ReadOnly] public NativeArray<AtmosNeighbours> Neighbours;

        public NativeArray<float> MolesWrite;
        public NativeArray<AtmosCellMeta> CellMetaWrite;

        [ReadOnly] public NativeArray<float> SpecificHeat;

        /// <summary>Per-cell thermal energy scratch (heatCapacity·T), seeded each run.</summary>
        public NativeArray<float> EnergyScratch;

        public int MaxGasTypes;
        public int GasTypeCount;
        public float DeltaTime;
        public float SpaceTemperature;

        public void Execute()
        {
            for (int i = 0; i < MolesRead.Length; i++)
                MolesWrite[i] = MolesRead[i];

            for (int i = 0; i < CellMeta.Length; i++)
                CellMetaWrite[i] = CellMeta[i];

            // Seed each cell's thermal energy from the read state so advected moles can carry
            // their enthalpy; temperatures are recomputed from this conserved energy afterwards.
            for (int c = 0; c < CellMeta.Length; c++)
            {
                float heatCapacity = AtmosThermo.HeatCapacity(MolesRead, SpecificHeat, c, MaxGasTypes, GasTypeCount);
                EnergyScratch[c] = heatCapacity * CellMeta[c].Temperature;
            }

            for (int activeIndex = 0; activeIndex < ActiveCells.Length; activeIndex++)
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

                    // Neighbour holds notably more pressure: it will drive flow toward us next
                    // tick, so wake it if it has gone dormant (otherwise the wave can't spread
                    // inward from a breach).
                    if (pressureDiff < -AtmosFluxConstants.PressureEpsilon)
                    {
                        WakeNeighbour(neighbourIndex, neighbour);
                        continue;
                    }

                    if (pressureDiff <= AtmosFluxConstants.PressureEpsilon)
                        continue;

                    for (int gasId = 0; gasId < GasTypeCount; gasId++)
                    {
                        int selfMoleIndex = GetMoleIndex(cellIndex, gasId);
                        int neighbourMoleIndex = GetMoleIndex(neighbourIndex, gasId);

                        float selfPartial = GetPartialPressure(MolesRead[selfMoleIndex], self.Temperature, self.Volume);
                        float neighbourPartial = GetPartialPressure(MolesRead[neighbourMoleIndex], neighbour.Temperature, neighbour.Volume);
                        float partialDiff = selfPartial - neighbourPartial;
                        if (partialDiff <= 0f)
                            continue;

                        // Venting into vacuum is unthrottled by the diffusion coefficient: there is
                        // no destination cell to overfill, so drain fast for a believable breach.
                        bool ventingToVacuum = neighbour.State == AtmosCellState.Vacuum;
                        float speed = ventingToVacuum
                            ? AtmosFluxConstants.VacuumVentSpeed
                            : AtmosFluxConstants.SimSpeed;

                        float molesToTransfer = partialDiff * 1000f * self.Volume /
                            (self.Temperature * AtmosFluxConstants.GasConstant);
                        molesToTransfer *= speed * DeltaTime;

                        // Never dump an entire cell through a single edge in one tick. A cell that
                        // fully empties has ~zero heat capacity, so its temperature loses any stable
                        // basis and thrashes between space temperature and whatever a neighbour
                        // refills it with. Capping the fraction keeps a stable gas core so the
                        // temperature decays smoothly (still fast: an exponential drain to vacuum).
                        float maxTransfer = ventingToVacuum
                            ? MolesWrite[selfMoleIndex] * AtmosFluxConstants.MaxVentFraction
                            : MolesWrite[selfMoleIndex];
                        molesToTransfer = math.min(molesToTransfer, maxTransfer);
                        if (molesToTransfer <= 0f)
                            continue;

                        // Moles leave the source carrying their enthalpy at the source temperature.
                        float energyMoved = molesToTransfer * SpecificHeat[gasId] * self.Temperature;

                        MolesWrite[selfMoleIndex] -= molesToTransfer;
                        EnergyScratch[cellIndex] -= energyMoved;
                        if (neighbour.State != AtmosCellState.Vacuum)
                        {
                            MolesWrite[neighbourMoleIndex] += molesToTransfer;
                            EnergyScratch[neighbourIndex] += energyMoved;
                        }

                        transferred = true;

                        AtmosCellMeta neighbourWrite = CellMetaWrite[neighbourIndex];
                        if (neighbourWrite.State != AtmosCellState.Vacuum)
                            neighbourWrite.State = AtmosCellState.Active;
                        CellMetaWrite[neighbourIndex] = neighbourWrite;
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

                    AtmosCellMeta settled = CellMetaWrite[cellIndex];
                    settled.State = AtmosCellState.Inactive;
                    CellMetaWrite[cellIndex] = settled;
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
                }

                // Any other state (Inactive / Vacuum / Blocked) is left untouched.
                CellMetaWrite[cellIndex] = selfWrite;
            }

            // Derive each cell's new temperature from its conserved energy and post-transfer
            // heat capacity. Cells that gained or lost moles change temperature; unchanged cells
            // resolve back to the same value (energy = heatCapacity · T).
            for (int c = 0; c < CellMetaWrite.Length; c++)
            {
                AtmosCellMeta meta = CellMetaWrite[c];
                if (meta.State == AtmosCellState.Blocked || meta.State == AtmosCellState.Vacuum)
                    continue;

                float heatCapacity = AtmosThermo.HeatCapacity(MolesWrite, SpecificHeat, c, MaxGasTypes, GasTypeCount);
                float totalMoles = AtmosThermo.TotalMoles(MolesWrite, c, MaxGasTypes, GasTypeCount);
                meta.Temperature = AtmosThermo.ResolveTemperature(EnergyScratch[c], heatCapacity, totalMoles, SpaceTemperature);
                CellMetaWrite[c] = meta;
            }
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
