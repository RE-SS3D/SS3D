using System;
using System.Collections.Generic;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Pure health simulation math — testable without Unity/FishNet.
    /// </summary>
    public static class HealthSimulation
    {
        public static float SumBleedingRates(IReadOnlyList<ZoneDamageState> zones)
        {
            float total = 0f;
            for (int i = 0; i < zones.Count; i++)
            {
                total += zones[i].BleedingRate;
            }

            return total;
        }

        public static float GetOrganFunction(IReadOnlyList<OrganState> organs, OrganType type)
        {
            for (int i = 0; i < organs.Count; i++)
            {
                if (organs[i].Type == type)
                {
                    return organs[i].FunctionPercent;
                }
            }

            return 100f;
        }

        public static float LungIntake(IReadOnlyList<OrganState> organs, float atmosphereO2 = 1f)
        {
            float left = GetOrganFunction(organs, OrganType.LeftLung) / 100f;
            float right = GetOrganFunction(organs, OrganType.RightLung) / 100f;
            return (left + right) * 0.5f * atmosphereO2 * HealthConstants.BaseOxygenDemand;
        }

        public static float HeartDelivery(IReadOnlyList<OrganState> organs, float bloodVolumeRatio)
        {
            float heart = GetOrganFunction(organs, OrganType.Heart) / 100f;
            return heart * bloodVolumeRatio * HealthConstants.BaseOxygenDemand;
        }

        public static float LiverClearance(IReadOnlyList<OrganState> organs)
        {
            float liver = GetOrganFunction(organs, OrganType.Liver) / 100f;
            float renal = liver * HealthConstants.RenalClearanceFactor;
            return (liver + renal) * HealthConstants.LiverClearanceRate;
        }

        public static SystemicPools TickPools(
            SystemicPools pools,
            IReadOnlyList<ZoneDamageState> zones,
            IReadOnlyList<OrganState> organs,
            float atmosphereO2 = 1f,
            float toxinIntake = HealthConstants.BaseToxinIntake)
        {
            float bloodDelta = -SumBleedingRates(zones) * HealthConstants.BleedingBloodDrainScale;
            float lungIntake = LungIntake(organs, atmosphereO2);
            float heartDelivery = HeartDelivery(organs, pools.BloodVolumeRatio);
            float oxyDelta = lungIntake - heartDelivery - HealthConstants.BaseOxygenDemand;

            if (pools.BloodVolumeRatio < HealthConstants.LowBloodThreshold)
            {
                float deficit = HealthConstants.LowBloodThreshold - pools.BloodVolumeRatio;
                oxyDelta += deficit * HealthConstants.LowBloodOxyDebtGainScale;
            }

            float toxinDelta = toxinIntake - LiverClearance(organs);

            return new SystemicPools
            {
                BloodVolumeRatio = Clamp01(pools.BloodVolumeRatio + bloodDelta),
                OxyDebt = Math.Max(0f, pools.OxyDebt + oxyDelta),
                ToxinConcentration = Math.Max(0f, pools.ToxinConcentration + toxinDelta),
            };
        }

        public static WoundSeverity SeverityFromBrute(float brute)
        {
            if (brute >= HealthConstants.DisabledThreshold) return WoundSeverity.Disabled;
            if (brute >= HealthConstants.SevereThreshold) return WoundSeverity.Severe;
            if (brute >= HealthConstants.WoundThreshold) return WoundSeverity.Wound;
            if (brute >= HealthConstants.BruisedThreshold) return WoundSeverity.Bruised;
            return WoundSeverity.None;
        }

        public static float BleedingRateForSeverity(WoundSeverity severity)
        {
            return severity switch
            {
                WoundSeverity.Wound => 0.5f,
                WoundSeverity.Severe => 1f,
                WoundSeverity.Disabled => 1.5f,
                WoundSeverity.Severed => 2f,
                _ => 0f,
            };
        }

        public static bool IsSystemicallyCritical(SystemicPools pools)
        {
            return pools.BloodVolumeRatio <= HealthConstants.CriticalBloodVolumeRatio
                || pools.OxyDebt >= HealthConstants.CriticalOxyDebt
                || pools.ToxinConcentration >= HealthConstants.CriticalToxinConcentration;
        }

        public static bool IsBrainCriticallyLow(float brainFunctionPercent)
        {
            return brainFunctionPercent <= HealthConstants.CriticalBrainFunctionPercent;
        }

        public static bool IsBrainDead(float brainFunctionPercent)
        {
            return brainFunctionPercent <= 0f;
        }

        public static bool IsCardiacArrest(float heartFunctionPercent)
        {
            return heartFunctionPercent <= 0f;
        }

        public static bool IsConscious(float brainFunctionPercent)
        {
            return brainFunctionPercent > HealthConstants.ConsciousnessBrainFunctionPercent;
        }

        public static HealthState EvaluateHealthState(
            SystemicPools pools,
            IReadOnlyList<OrganState> organs)
        {
            float brain = GetOrganFunction(organs, OrganType.Brain);
            float heart = GetOrganFunction(organs, OrganType.Heart);

            if (IsBrainDead(brain))
            {
                return HealthState.Dead;
            }

            if (IsCardiacArrest(heart))
            {
                return HealthState.CardiacArrest;
            }

            if (IsSystemicallyCritical(pools) || IsBrainCriticallyLow(brain))
            {
                return HealthState.Critical;
            }

            return HealthState.Healthy;
        }

        public static HealthSnapshot BuildSnapshot(
            SystemicPools pools,
            IReadOnlyList<ZoneDamageState> zones,
            IReadOnlyList<OrganState> organs)
        {
            float worstBrute = 0f;
            float worstBurn = 0f;
            bool bleeding = false;

            for (int i = 0; i < zones.Count; i++)
            {
                worstBrute = Math.Max(worstBrute, zones[i].Brute);
                worstBurn = Math.Max(worstBurn, zones[i].Burn);
                if (zones[i].BleedingRate > 0f)
                {
                    bleeding = true;
                }
            }

            float brain = GetOrganFunction(organs, OrganType.Brain);
            float heart = GetOrganFunction(organs, OrganType.Heart);
            HealthState state = EvaluateHealthState(pools, organs);

            return new HealthSnapshot
            {
                State = state,
                Pools = pools,
                WorstZoneBrute = worstBrute,
                WorstZoneBurn = worstBurn,
                IsBleeding = bleeding,
                IsConscious = IsConscious(brain),
                IsCardiacArrest = IsCardiacArrest(heart),
            };
        }

        private static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
    }
}
