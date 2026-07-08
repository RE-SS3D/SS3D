using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SS3D.Systems.Atmospherics.ECS
{
    /// <summary>
    /// Per-cell gas reactions (currently plasma combustion). Runs in place over the active list —
    /// reactions are local, so no double buffering is needed. Above the ignition temperature,
    /// plasma burns with oxygen into carbon dioxide, releasing heat that raises the cell
    /// temperature (recomputed from total thermal energy) and keeps the fire self-sustaining.
    /// </summary>
    [BurstCompile]
    public struct ReactAtmosJob : IJob
    {
        // Scalar fields first — keeps Burst job struct layout stable when native buffers are added.
        public int MaxGasTypes;
        public int GasTypeCount;
        public int OxygenId;
        public int PlasmaId;
        public int CarbonDioxideId;
        public float DeltaTime;

        [ReadOnly] public NativeArray<int> ActiveCells;
        [ReadOnly] public NativeArray<float> SpecificHeat;
        public NativeArray<float> Moles;
        public NativeArray<AtmosCellMeta> CellMeta;
        public NativeArray<float> BurnIntensity;

        public void Execute()
        {
            int cellCount = MaxGasTypes > 0 ? Moles.Length / MaxGasTypes : 0;
            int safeGasTypeCount = math.min(GasTypeCount, MaxGasTypes);

            if (cellCount <= 0 ||
                !IsGasIdValid(OxygenId) ||
                !IsGasIdValid(PlasmaId) ||
                !IsGasIdValid(CarbonDioxideId))
                return;

            for (int activeIndex = 0; activeIndex < ActiveCells.Length; activeIndex++)
            {
                int cell = ActiveCells[activeIndex];
                if (cell < 0 || cell >= cellCount)
                    continue;

                AtmosCellMeta meta = CellMeta[cell];
                if (!meta.IsSimulated)
                    continue;

                if (meta.Temperature < AtmosFluxConstants.PlasmaIgnitionTemperature)
                    continue;

                int baseIndex = cell * MaxGasTypes;
                float plasma = Moles[baseIndex + PlasmaId];
                float oxygen = Moles[baseIndex + OxygenId];
                if (plasma < AtmosFluxConstants.MinimumBurnMoles || oxygen < AtmosFluxConstants.MinimumBurnMoles)
                    continue;

                // Hotter fires burn a larger fraction of the available plasma per tick.
                float temperatureScale = math.saturate(
                    (meta.Temperature - AtmosFluxConstants.PlasmaIgnitionTemperature) /
                    AtmosFluxConstants.PlasmaIgnitionTemperature);
                float burnFraction = AtmosFluxConstants.PlasmaBurnRate * (0.25f + temperatureScale) * DeltaTime;

                float plasmaBurn = plasma * burnFraction;
                float oxygenBurn = plasmaBurn * AtmosFluxConstants.OxygenPerPlasma;

                // Oxygen-starved fires burn only as much plasma as the available oxygen allows.
                if (oxygenBurn > oxygen)
                {
                    oxygenBurn = oxygen;
                    plasmaBurn = oxygen / AtmosFluxConstants.OxygenPerPlasma;
                }

                plasmaBurn = math.min(plasmaBurn, plasma);
                if (plasmaBurn < AtmosFluxConstants.MinimumBurnMoles)
                    continue;

                float energyBefore = AtmosThermo.HeatCapacity(Moles, SpecificHeat, cell, MaxGasTypes, safeGasTypeCount)
                    * meta.Temperature;

                Moles[baseIndex + PlasmaId] = plasma - plasmaBurn;
                Moles[baseIndex + OxygenId] = oxygen - oxygenBurn;
                Moles[baseIndex + CarbonDioxideId] += plasmaBurn;

                float energyAfter = energyBefore + plasmaBurn * AtmosFluxConstants.FireEnergyPerMole;
                float heatCapacityAfter = AtmosThermo.HeatCapacity(Moles, SpecificHeat, cell, MaxGasTypes, safeGasTypeCount);
                if (heatCapacityAfter > HeatCapacityEpsilon)
                    meta.Temperature = energyAfter / heatCapacityAfter;

                meta.State = AtmosCellState.Active;
                CellMeta[cell] = meta;

                if (BurnIntensity.IsCreated && DeltaTime > 0f)
                {
                    // Normalize burn rate into a stable 0–1 \"fire intensity\" range so the
                    // visualization can treat 1 as a strong plasma fire. With the current
                    // tuning a hot plasma tile burns on the order of 4–5 mol/s, so we scale
                    // by ~0.16 and clamp.
                    float burnRate = plasmaBurn / DeltaTime;
                    float fireIntensity = math.saturate(burnRate * 0.16f);
                    BurnIntensity[cell] = math.max(BurnIntensity[cell], fireIntensity);
                }
            }
        }

        private bool IsGasIdValid(int gasId) => gasId >= 0 && gasId < MaxGasTypes;

        private const float HeatCapacityEpsilon = 1e-6f;
    }
}
