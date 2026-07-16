using Unity.Collections;

namespace SS3D.Systems.Atmospherics.ECS
{
    /// <summary>
    /// Shared thermodynamic helpers for the Burst atmos jobs. Energy is tracked as
    /// <c>E = heatCapacity · T</c> where <c>heatCapacity = Σ molesᵢ · specificHeatᵢ</c>,
    /// so temperature is always derived from conserved energy rather than transferred directly.
    /// </summary>
    public static class AtmosThermo
    {
        /// <summary>Total heat capacity of a cell's gas mixture (J/K in tuning units).</summary>
        public static float HeatCapacity(
            in NativeArray<float> moles,
            in NativeArray<float> specificHeat,
            int cellIndex,
            int maxGasTypes,
            int gasTypeCount)
        {
            if (maxGasTypes <= 0 || moles.Length == 0)
                return 0f;

            int cellCount = moles.Length / maxGasTypes;
            if (cellIndex < 0 || cellIndex >= cellCount)
                return 0f;

            int safeGasTypeCount = gasTypeCount < maxGasTypes ? gasTypeCount : maxGasTypes;
            if (safeGasTypeCount <= 0)
                return 0f;

            float capacity = 0f;
            int baseIndex = cellIndex * maxGasTypes;
            for (int gasId = 0; gasId < safeGasTypeCount; gasId++)
                capacity += moles[baseIndex + gasId] * specificHeat[gasId];

            return capacity;
        }

        /// <summary>Total moles of gas in a cell across every tracked type.</summary>
        public static float TotalMoles(
            in NativeArray<float> moles,
            int cellIndex,
            int maxGasTypes,
            int gasTypeCount)
        {
            if (maxGasTypes <= 0 || moles.Length == 0)
                return 0f;

            int cellCount = moles.Length / maxGasTypes;
            if (cellIndex < 0 || cellIndex >= cellCount)
                return 0f;

            int safeGasTypeCount = gasTypeCount < maxGasTypes ? gasTypeCount : maxGasTypes;
            if (safeGasTypeCount <= 0)
                return 0f;

            float total = 0f;
            int baseIndex = cellIndex * maxGasTypes;
            for (int gasId = 0; gasId < safeGasTypeCount; gasId++)
                total += moles[baseIndex + gasId];

            return total;
        }

        /// <summary>
        /// Derives a cell temperature from its conserved energy and heat capacity, blended toward
        /// <paramref name="spaceTemperature"/> as the cell empties. A near-vacuum cell has almost no
        /// heat capacity, so a raw <c>E / C</c> divides two near-zero numbers and amplifies advection
        /// and rounding noise into wild temperature swings. We add a virtual space-temperature
        /// reservoir whose weight fades in only for near-empty cells, keeping full cells (and sealed
        /// rooms) energy-conserving while making vented cells resolve stably to space temperature.
        /// </summary>
        public static float ResolveTemperature(
            float energy,
            float heatCapacity,
            float totalMoles,
            float spaceTemperature)
        {
            float floorFraction = 1f - (totalMoles / AtmosFluxConstants.VacuumFadeMoles);
            if (floorFraction < 0f)
                floorFraction = 0f;
            else if (floorFraction > 1f)
                floorFraction = 1f;

            float floor = AtmosFluxConstants.VacuumHeatCapacityFloor * floorFraction;
            float denominator = heatCapacity + floor;
            if (denominator <= 0f)
                return spaceTemperature;

            return (energy + (floor * spaceTemperature)) / denominator;
        }
    }
}
