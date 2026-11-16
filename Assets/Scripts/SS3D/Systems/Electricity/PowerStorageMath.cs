using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Shared kW/kWh charge/discharge helpers for APC cells, SMES, and batteries.
    /// </summary>
    public static class PowerStorageMath
    {
        public static float MaxDeliverableKw(float storedEnergyKwh, float maxDischargeRateKw, float tickSeconds, bool isOn = true)
        {
            if (!isOn || storedEnergyKwh <= 0f)
            {
                return 0f;
            }

            return Mathf.Min(maxDischargeRateKw, ElectricityUnits.KwhToKw(storedEnergyKwh, tickSeconds));
        }

        public static float AddPowerKw(
            ref float storedEnergyKwh,
            float maxCapacityKwh,
            float maxChargeRateKw,
            float requestedKw,
            float tickSeconds,
            bool isOn = true)
        {
            float remainingCapacityKwh = Mathf.Max(0f, maxCapacityKwh - storedEnergyKwh);
            if (requestedKw <= 0f || !isOn || remainingCapacityKwh <= 0f || maxChargeRateKw <= 0f)
            {
                return 0f;
            }

            float absorbedKw = Mathf.Min(requestedKw, maxChargeRateKw);
            float energyToAdd = ElectricityUnits.KwToKwh(absorbedKw, tickSeconds);
            float addedEnergy = Mathf.Min(remainingCapacityKwh, energyToAdd);
            storedEnergyKwh += addedEnergy;
            return ElectricityUnits.KwhToKw(addedEnergy, tickSeconds);
        }

        public static float RemovePowerKw(
            ref float storedEnergyKwh,
            float maxDischargeRateKw,
            float requestedKw,
            float tickSeconds,
            bool isOn = true)
        {
            if (requestedKw <= 0f || storedEnergyKwh <= 0f)
            {
                return 0f;
            }

            float deliverableKw = MaxDeliverableKw(storedEnergyKwh, maxDischargeRateKw, tickSeconds, isOn);
            float deliveredKw = Mathf.Min(requestedKw, deliverableKw);
            storedEnergyKwh -= ElectricityUnits.KwToKwh(deliveredKw, tickSeconds);
            return deliveredKw;
        }
    }
}
