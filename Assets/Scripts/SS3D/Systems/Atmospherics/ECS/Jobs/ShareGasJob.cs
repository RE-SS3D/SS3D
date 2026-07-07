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

        public int MaxGasTypes;
        public int GasTypeCount;
        public float DeltaTime;

        public void Execute()
        {
            for (int i = 0; i < MolesRead.Length; i++)
                MolesWrite[i] = MolesRead[i];

            for (int i = 0; i < CellMeta.Length; i++)
                CellMetaWrite[i] = CellMeta[i];

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
                    if (neighbourIndex < 0)
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
                        float speed = neighbour.State == AtmosCellState.Vacuum
                            ? AtmosFluxConstants.VacuumVentSpeed
                            : AtmosFluxConstants.SimSpeed;

                        float molesToTransfer = partialDiff * 1000f * self.Volume /
                            (self.Temperature * AtmosFluxConstants.GasConstant);
                        molesToTransfer *= speed * DeltaTime;
                        molesToTransfer = math.min(molesToTransfer, MolesWrite[selfMoleIndex]);
                        if (molesToTransfer <= 0f)
                            continue;

                        MolesWrite[selfMoleIndex] -= molesToTransfer;
                        if (neighbour.State != AtmosCellState.Vacuum)
                            MolesWrite[neighbourMoleIndex] += molesToTransfer;

                        transferred = true;

                        AtmosCellMeta neighbourWrite = CellMetaWrite[neighbourIndex];
                        if (neighbourWrite.State != AtmosCellState.Vacuum)
                            neighbourWrite.State = AtmosCellState.Active;
                        CellMetaWrite[neighbourIndex] = neighbourWrite;
                    }
                }

                // If we moved gas this tick our pressure changed, so every dormant neighbour
                // should re-check next tick. This lets the active front advance one tile per
                // tick instead of waiting for the pressure gap to slowly build past epsilon.
                if (transferred)
                {
                    for (int direction = 0; direction < 4; direction++)
                    {
                        int neighbourIndex = Neighbours[cellIndex].Get(direction);
                        if (neighbourIndex < 0)
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
