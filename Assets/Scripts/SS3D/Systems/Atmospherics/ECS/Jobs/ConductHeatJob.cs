using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SS3D.Systems.Atmospherics.ECS
{
    /// <summary>
    /// Heat conduction across open cell edges, independent of pressure. Runs as an energy-conserving
    /// Jacobi gather: each undirected edge is processed exactly once (lower index owns it when both
    /// endpoints are simulated), moving energy — not temperature — so total <c>Σ heatCapacity·T</c>
    /// is preserved. Only temperature and wake-state are written; moles are untouched.
    /// </summary>
    [BurstCompile]
    public struct ConductHeatJob : IJob
    {
        [ReadOnly] public NativeArray<int> ActiveCells;
        [ReadOnly] public NativeArray<float> Moles;
        [ReadOnly] public NativeArray<AtmosCellMeta> CellMeta;
        [ReadOnly] public NativeArray<AtmosNeighbours> Neighbours;
        [ReadOnly] public NativeArray<float> SpecificHeat;

        public NativeArray<AtmosCellMeta> CellMetaWrite;
        public NativeArray<float> EnergyScratch;

        public int MaxGasTypes;
        public int GasTypeCount;
        public float DeltaTime;
        public float SpaceTemperature;

        public void Execute()
        {
            for (int i = 0; i < CellMeta.Length; i++)
                CellMetaWrite[i] = CellMeta[i];

            for (int c = 0; c < CellMeta.Length; c++)
            {
                float heatCapacity = AtmosThermo.HeatCapacity(Moles, SpecificHeat, c, MaxGasTypes, GasTypeCount);
                EnergyScratch[c] = heatCapacity * CellMeta[c].Temperature;
            }

            float rate = AtmosFluxConstants.HeatConductionRate * DeltaTime;

            for (int activeIndex = 0; activeIndex < ActiveCells.Length; activeIndex++)
            {
                int a = ActiveCells[activeIndex];
                AtmosCellMeta selfMeta = CellMeta[a];
                if (selfMeta.State == AtmosCellState.Blocked || selfMeta.State == AtmosCellState.Vacuum)
                    continue;

                float heatCapacityA = AtmosThermo.HeatCapacity(Moles, SpecificHeat, a, MaxGasTypes, GasTypeCount);
                if (heatCapacityA <= HeatCapacityEpsilon)
                    continue;

                float temperatureA = selfMeta.Temperature;
                bool conducted = false;

                for (int direction = 0; direction < 4; direction++)
                {
                    int b = Neighbours[a].Get(direction);
                    if (b < 0 || b >= CellMeta.Length)
                        continue;

                    AtmosCellMeta neighbourMeta = CellMeta[b];
                    if (neighbourMeta.State == AtmosCellState.Blocked || neighbourMeta.State == AtmosCellState.Vacuum)
                        continue;

                    // Each edge is handled once: when both cells are simulated (both iterate), the
                    // lower index owns it; when the neighbour is dormant, this active cell owns it.
                    bool neighbourSimulated = neighbourMeta.IsSimulated;
                    if (neighbourSimulated && a > b)
                        continue;

                    float heatCapacityB = AtmosThermo.HeatCapacity(Moles, SpecificHeat, b, MaxGasTypes, GasTypeCount);
                    if (heatCapacityB <= HeatCapacityEpsilon)
                        continue;

                    float deltaTemperature = neighbourMeta.Temperature - temperatureA;
                    if (deltaTemperature < AtmosFluxConstants.ThermalEpsilon &&
                        deltaTemperature > -AtmosFluxConstants.ThermalEpsilon)
                        continue;

                    // Harmonic-mean heat capacity bounds the temperature change on both sides.
                    float energyIntoA = rate * deltaTemperature *
                        (heatCapacityA * heatCapacityB / (heatCapacityA + heatCapacityB));

                    EnergyScratch[a] += energyIntoA;
                    EnergyScratch[b] -= energyIntoA;
                    conducted = true;

                    // Heat spreading into a settled neighbour wakes it so conduction can continue.
                    if (!neighbourSimulated)
                    {
                        AtmosCellMeta neighbourWrite = CellMetaWrite[b];
                        neighbourWrite.State = AtmosCellState.Active;
                        CellMetaWrite[b] = neighbourWrite;
                    }
                }

                if (conducted)
                {
                    AtmosCellMeta selfWrite = CellMetaWrite[a];
                    selfWrite.State = AtmosCellState.Active;
                    CellMetaWrite[a] = selfWrite;
                }
            }

            // Resolve conduction results back into temperatures. Near-empty cells blend toward
            // space temperature instead of dividing energy by ~0, so a vented cell settles stably
            // to space temperature rather than oscillating.
            for (int c = 0; c < CellMetaWrite.Length; c++)
            {
                AtmosCellMeta meta = CellMetaWrite[c];
                if (meta.State == AtmosCellState.Blocked || meta.State == AtmosCellState.Vacuum)
                    continue;

                float heatCapacity = AtmosThermo.HeatCapacity(Moles, SpecificHeat, c, MaxGasTypes, GasTypeCount);
                float totalMoles = AtmosThermo.TotalMoles(Moles, c, MaxGasTypes, GasTypeCount);
                meta.Temperature = AtmosThermo.ResolveTemperature(EnergyScratch[c], heatCapacity, totalMoles, SpaceTemperature);
                CellMetaWrite[c] = meta;
            }

            // Radiative loss to space across open vacuum edges, over the active list only (an
            // exposed cell stays active until it reaches space temperature). Modeled as a direct
            // temperature relaxation because energy leaving to space is not conserved — this keeps
            // cooling a cell even after it has vented to near-vacuum, where an energy-based update
            // would divide by ~0 and a breached hot tile would otherwise freeze at its fire heat.
            for (int activeIndex = 0; activeIndex < ActiveCells.Length; activeIndex++)
            {
                int c = ActiveCells[activeIndex];
                AtmosCellMeta meta = CellMetaWrite[c];
                if (meta.State == AtmosCellState.Blocked || meta.State == AtmosCellState.Vacuum)
                    continue;

                if (meta.Temperature <= SpaceTemperature + AtmosFluxConstants.ThermalEpsilon || !IsExposedToVacuum(c))
                    continue;

                meta.Temperature += AtmosFluxConstants.SpaceConductionRate * DeltaTime *
                    (SpaceTemperature - meta.Temperature);
                meta.State = AtmosCellState.Active;
                CellMetaWrite[c] = meta;
            }
        }

        private bool IsExposedToVacuum(int cellIndex)
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

        private const float HeatCapacityEpsilon = 1e-6f;
    }
}
