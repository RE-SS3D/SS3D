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
                    if (selfPressure - neighbourPressure <= AtmosFluxConstants.PressureEpsilon)
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

                        float molesToTransfer = partialDiff * 1000f * self.Volume /
                            (self.Temperature * AtmosFluxConstants.GasConstant);
                        molesToTransfer *= AtmosFluxConstants.SimSpeed * DeltaTime;
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

                AtmosCellMeta selfWrite = CellMetaWrite[cellIndex];
                if (transferred)
                    selfWrite.State = AtmosCellState.Active;
                else if (selfWrite.State == AtmosCellState.Active)
                    selfWrite.State = AtmosCellState.Semiactive;
                CellMetaWrite[cellIndex] = selfWrite;
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
