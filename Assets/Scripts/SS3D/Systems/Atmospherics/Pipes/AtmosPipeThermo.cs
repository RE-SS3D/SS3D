namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Thermodynamic helpers for pooled gas pipe networks.
    /// </summary>
    public static class AtmosPipeThermo
    {
        public static float GetTotalMoles(float[] moles, int gasTypeCount)
        {
            if (moles == null || gasTypeCount <= 0)
                return 0f;

            float total = 0f;
            int count = gasTypeCount < moles.Length ? gasTypeCount : moles.Length;
            for (int i = 0; i < count; i++)
                total += moles[i];

            return total;
        }

        public static float GetHeatCapacity(float[] moles, float[] specificHeats, int gasTypeCount)
        {
            if (moles == null || specificHeats == null || gasTypeCount <= 0)
                return 0f;

            float capacity = 0f;
            int count = gasTypeCount;
            if (count > moles.Length)
                count = moles.Length;
            if (count > specificHeats.Length)
                count = specificHeats.Length;

            for (int gasId = 0; gasId < count; gasId++)
                capacity += moles[gasId] * specificHeats[gasId];

            return capacity;
        }

        public static float GetPressure(float[] moles, float temperature, float volume, int gasTypeCount)
        {
            if (volume <= 0f || temperature <= 0f)
                return 0f;

            float totalMoles = GetTotalMoles(moles, gasTypeCount);
            return totalMoles * ECS.AtmosFluxConstants.GasConstant * temperature / volume / 1000f;
        }

        public static float BlendTemperature(
            float currentTemperature,
            float currentHeatCapacity,
            float addedMoles,
            float addedSpecificHeat,
            float addedTemperature)
        {
            if (addedMoles <= 0f)
                return currentTemperature;

            float addedCapacity = addedMoles * addedSpecificHeat;
            float totalCapacity = currentHeatCapacity + addedCapacity;
            if (totalCapacity <= 0f)
                return addedTemperature;

            float currentEnergy = currentHeatCapacity * currentTemperature;
            float addedEnergy = addedCapacity * addedTemperature;
            return (currentEnergy + addedEnergy) / totalCapacity;
        }
    }
}
