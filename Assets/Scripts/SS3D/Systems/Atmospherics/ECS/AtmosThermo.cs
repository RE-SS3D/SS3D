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
            float capacity = 0f;
            int baseIndex = cellIndex * maxGasTypes;
            for (int gasId = 0; gasId < gasTypeCount; gasId++)
                capacity += moles[baseIndex + gasId] * specificHeat[gasId];

            return capacity;
        }
    }
}
