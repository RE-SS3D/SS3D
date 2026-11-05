using System;
using SS3D.Systems.Atmospherics.Pipes;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Pure helpers for turf → health exposure: breathability, toxin intake, env burn, alerts.
    /// Sampling that needs SubSystems lives on <see cref="HumanHealthController"/>.
    /// </summary>
    public static class HealthEnvironmentExposure
    {
        /// <summary>O₂ partial pressure in kPa (mole fraction × total pressure).</summary>
        public static float OxygenPartialPressureKpa(float oxygenMoleFraction, float pressureKpa)
        {
            if (pressureKpa <= 0f || oxygenMoleFraction <= 0f)
            {
                return 0f;
            }

            return oxygenMoleFraction * pressureKpa;
        }

        /// <summary>
        /// 1 at/above comfortable PO₂, 0 at/below unbreathable. Station air (~20 kPa PO₂) stays full.
        /// </summary>
        public static float NormalizeBreathability(float oxygenMoleFraction, float pressureKpa, bool isVacuum)
        {
            if (isVacuum || pressureKpa <= 0f)
            {
                return 0f;
            }

            float po2 = OxygenPartialPressureKpa(oxygenMoleFraction, pressureKpa);
            if (po2 >= HealthConstants.OxygenPartialPressureComfortableKpa)
            {
                return 1f;
            }

            if (po2 <= HealthConstants.OxygenPartialPressureUnbreathableKpa)
            {
                return 0f;
            }

            return Mathf.InverseLerp(
                HealthConstants.OxygenPartialPressureUnbreathableKpa,
                HealthConstants.OxygenPartialPressureComfortableKpa,
                po2);
        }

        public static float ToxinIntakeFromPlasma(float plasmaMoleFraction)
        {
            if (plasmaMoleFraction <= 0f)
            {
                return HealthConstants.BaseToxinIntake;
            }

            return plasmaMoleFraction * HealthConstants.PlasmaToxinIntakeScale;
        }

        /// <summary>
        /// Per-zone burn from ambient temperature and fire. Pressure is not burn —
        /// use <see cref="PressureLungDamage"/> for barotrauma. A seal stops cold and
        /// scales heat and fire down by <paramref name="heatProtection"/> (armor.md §1, §3).
        /// </summary>
        public static float EnvironmentalBurnDamage(
            float temperatureKelvin,
            float burnIntensity,
            bool isSealed = false,
            float heatProtection = 0f)
        {
            float burn = 0f;

            if (temperatureKelvin >= AirAlarmConstants.HighTemperatureKelvin)
            {
                burn += (temperatureKelvin - AirAlarmConstants.HighTemperatureKelvin)
                    * HealthConstants.HotDamageBurnPerKelvin;
            }

            if (!isSealed && temperatureKelvin <= HealthConstants.ColdDamageTemperatureKelvin)
            {
                burn += (HealthConstants.ColdDamageTemperatureKelvin - temperatureKelvin)
                    * HealthConstants.ColdDamageBurnPerKelvin;
            }

            if (burnIntensity > 0f)
            {
                burn += burnIntensity * HealthConstants.FireBurnPerIntensity;
            }

            float exposed = Math.Min(burn, HealthConstants.MaxEnvironmentalBurnPerZonePerTick);
            return isSealed ? exposed * (1f - Mathf.Clamp01(heatProtection)) : exposed;
        }

        /// <summary>
        /// Lung function drain from vacuum / low / high pressure (barotrauma).
        /// Station pressure returns 0. Applied equally to left and right lung. A seal stops it.
        /// </summary>
        public static float PressureLungDamage(float pressureKpa, bool isVacuum, bool isSealed = false)
        {
            if (isSealed)
            {
                return 0f;
            }

            if (isVacuum || pressureKpa <= 0f)
            {
                return Math.Min(
                    HealthConstants.VacuumLungDamagePerTick,
                    HealthConstants.MaxPressureLungDamagePerTick);
            }

            float damage = 0f;

            if (pressureKpa < AirAlarmConstants.LowPressureKpa)
            {
                damage += (AirAlarmConstants.LowPressureKpa - pressureKpa)
                    * HealthConstants.LowPressureLungDamagePerKpa;
            }

            if (pressureKpa > AirAlarmConstants.HighPressureKpa)
            {
                damage += (pressureKpa - AirAlarmConstants.HighPressureKpa)
                    * HealthConstants.HighPressureLungDamagePerKpa;
            }

            return Math.Min(damage, HealthConstants.MaxPressureLungDamagePerTick);
        }

        public static HealthEnvironmentState FromTileSample(
            AtmosAreaSample sample,
            bool isVacuum,
            float burnIntensity)
        {
            float breathability = NormalizeBreathability(
                sample.OxygenMoleFraction,
                sample.AveragePressureKpa,
                isVacuum);

            return new HealthEnvironmentState
            {
                HasSample = true,
                IsVacuum = isVacuum,
                AtmosphereBreathability = breathability,
                TemperatureKelvin = sample.TemperatureKelvin,
                PressureKpa = sample.AveragePressureKpa,
                BurnIntensity = burnIntensity,
                PlasmaMoleFraction = sample.PlasmaMoleFraction,
                OxygenMoleFraction = sample.OxygenMoleFraction,
            };
        }

        public static HealthEnvironmentState FromVacuumCell(float temperatureKelvin, float burnIntensity)
        {
            return new HealthEnvironmentState
            {
                HasSample = true,
                IsVacuum = true,
                AtmosphereBreathability = 0f,
                TemperatureKelvin = temperatureKelvin,
                PressureKpa = 0f,
                BurnIntensity = burnIntensity,
                PlasmaMoleFraction = 0f,
                OxygenMoleFraction = 0f,
            };
        }

        /// <summary>
        /// Breath moles of O₂ to pull this tick (0 when unbreathable / disabled).
        /// </summary>
        public static float BreathOxygenMoles(float atmosphereBreathability, float lungFunction01)
        {
            if (atmosphereBreathability <= 0f || lungFunction01 <= 0f)
            {
                return 0f;
            }

            return HealthConstants.BreathOxygenMolesPerTick
                * atmosphereBreathability
                * Mathf.Clamp01(lungFunction01);
        }
    }
}
