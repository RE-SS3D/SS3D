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

        public static float GetOrganFunction(IReadOnlyList<OrganState> organs, OrganType type, float bloodVolumeRatio = 1f)
        {
            float stored = OrganSimulation.GetStoredOrganFunction(organs, type);
            return OrganSimulation.EffectiveOrganFunction(stored, bloodVolumeRatio);
        }

        public static float LungIntake(IReadOnlyList<OrganState> organs, float atmosphereO2 = 1f, float bloodVolumeRatio = 1f)
        {
            float left = GetOrganFunction(organs, OrganType.LeftLung, bloodVolumeRatio) / 100f;
            float right = GetOrganFunction(organs, OrganType.RightLung, bloodVolumeRatio) / 100f;
            return (left + right) * 0.5f * atmosphereO2 * HealthConstants.BaseOxygenDemand;
        }

        public static float HeartDelivery(IReadOnlyList<OrganState> organs, float bloodVolumeRatio)
        {
            float heart = GetOrganFunction(organs, OrganType.Heart, bloodVolumeRatio) / 100f;
            float deliveryVolume = MathF.Pow(Math.Max(bloodVolumeRatio, 0f), HealthConstants.BloodDeliveryVolumeExponent);
            return heart * deliveryVolume * HealthConstants.BaseOxygenDemand;
        }

        public static float LiverClearance(IReadOnlyList<OrganState> organs, float bloodVolumeRatio = 1f)
        {
            float liver = GetOrganFunction(organs, OrganType.Liver, bloodVolumeRatio) / 100f;
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
            float lungIntake = LungIntake(organs, atmosphereO2, pools.BloodVolumeRatio);
            float heartDelivery = HeartDelivery(organs, pools.BloodVolumeRatio);
            float oxyDelta = lungIntake - heartDelivery - HealthConstants.BaseOxygenDemand;

            // Hemorrhagic hypoxia scales continuously with blood lost — shock before empty.
            oxyDelta += (1f - pools.BloodVolumeRatio) * HealthConstants.LowBloodOxyDebtGainScale;

            float toxinDelta = toxinIntake - LiverClearance(organs, pools.BloodVolumeRatio);

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

        public static WoundSeverity SeverityFromBurn(float burn)
        {
            if (burn >= HealthConstants.BurnDisabledThreshold) return WoundSeverity.Disabled;
            if (burn >= HealthConstants.BurnSevereThreshold) return WoundSeverity.Severe;
            if (burn >= HealthConstants.BurnWoundThreshold) return WoundSeverity.Wound;
            return WoundSeverity.None;
        }

        public static WoundSeverity ResolveZoneSeverity(float brute, float burn)
        {
            WoundSeverity bruteSeverity = SeverityFromBrute(brute);
            WoundSeverity burnSeverity = SeverityFromBurn(burn);
            return (WoundSeverity)Math.Max((int)bruteSeverity, (int)burnSeverity);
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

        public static bool IsSeverableZone(BodyZone zone)
        {
            return zone is BodyZone.Head
                or BodyZone.LeftArm
                or BodyZone.RightArm
                or BodyZone.LeftLeg
                or BodyZone.RightLeg;
        }

        public static void ApplySeverance(ref ZoneDamageState state)
        {
            state.IsSevered = true;
            state.Severity = WoundSeverity.Severed;
            state.IsDisabled = true;
            state.BleedingRate = BleedingRateForSeverity(WoundSeverity.Severed);
        }

        public static void RefreshZoneDerivedState(ref ZoneDamageState state)
        {
            if (state.IsSevered)
            {
                state.Severity = WoundSeverity.Severed;
                state.IsDisabled = true;
                return;
            }

            state.Severity = ResolveZoneSeverity(state.Brute, state.Burn);
            state.IsDisabled = state.Severity >= WoundSeverity.Disabled;
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

        public static HealthCriticalFlags BuildCriticalFlags(SystemicPools pools, float brainEffectiveFunctionPercent)
        {
            HealthCriticalFlags flags = HealthCriticalFlags.None;

            if (pools.BloodVolumeRatio <= HealthConstants.CriticalBloodVolumeRatio)
            {
                flags |= HealthCriticalFlags.LowBlood;
            }

            if (pools.OxyDebt >= HealthConstants.CriticalOxyDebt)
            {
                flags |= HealthCriticalFlags.HighOxyDebt;
            }

            if (pools.ToxinConcentration >= HealthConstants.CriticalToxinConcentration)
            {
                flags |= HealthCriticalFlags.HighToxin;
            }

            if (IsBrainCriticallyLow(brainEffectiveFunctionPercent))
            {
                flags |= HealthCriticalFlags.LowBrain;
            }

            return flags;
        }

        public static HealthState EvaluateHealthState(
            SystemicPools pools,
            IReadOnlyList<OrganState> organs)
        {
            float bloodVolume = pools.BloodVolumeRatio;
            float brainStored = OrganSimulation.GetStoredOrganFunction(organs, OrganType.Brain);
            float heartStored = OrganSimulation.GetStoredOrganFunction(organs, OrganType.Heart);
            float brainEffective = GetOrganFunction(organs, OrganType.Brain, bloodVolume);

            if (IsBrainDead(brainStored))
            {
                return HealthState.Dead;
            }

            if (IsCardiacArrest(heartStored))
            {
                return HealthState.CardiacArrest;
            }

            if (IsSystemicallyCritical(pools) || IsBrainCriticallyLow(brainEffective))
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
            int bleedingMask = 0;
            int severedMask = 0;
            int bleedingRatePacked = 0;
            float totalBleedingRate = 0f;

            for (int i = 0; i < zones.Count; i++)
            {
                worstBrute = Math.Max(worstBrute, zones[i].Brute);
                worstBurn = Math.Max(worstBurn, zones[i].Burn);
                float zoneBleed = zones[i].BleedingRate;
                if (zoneBleed > 0f)
                {
                    bleeding = true;
                    bleedingMask |= 1 << i;
                    totalBleedingRate += zoneBleed;
                }

                if (i < HealthConstants.ZoneCount)
                {
                    int code = (int)Math.Round(zoneBleed * 2f);
                    if (code < 0)
                    {
                        code = 0;
                    }
                    else if (code > 7)
                    {
                        code = 7;
                    }

                    bleedingRatePacked |= code << (i * 3);
                }

                if (zones[i].IsSevered)
                {
                    severedMask |= 1 << i;
                }
            }

            float brainStored = OrganSimulation.GetStoredOrganFunction(organs, OrganType.Brain);
            float heartStored = OrganSimulation.GetStoredOrganFunction(organs, OrganType.Heart);
            float brainEffective = GetOrganFunction(organs, OrganType.Brain, pools.BloodVolumeRatio);
            HealthState state = EvaluateHealthState(pools, organs);

            return new HealthSnapshot
            {
                State = state,
                Pools = pools,
                WorstZoneBrute = worstBrute,
                WorstZoneBurn = worstBurn,
                IsBleeding = bleeding,
                IsConscious = IsConscious(brainEffective),
                IsCardiacArrest = IsCardiacArrest(heartStored),
                BleedingZoneMask = bleedingMask,
                SeveredZoneMask = severedMask,
                BleedingRatePacked = bleedingRatePacked,
                TotalBleedingRate = totalBleedingRate,
                BrainFunctionPercent = brainStored,
                HeartFunctionPercent = heartStored,
                MovementSpeedMultiplier = OrganSimulation.ComputeMovementSpeedMultiplier(zones),
                CanUseArms = OrganSimulation.CanUseArms(zones),
                CriticalFlags = BuildCriticalFlags(pools, brainEffective),
                CanDefibrillate = IsCardiacArrest(heartStored) && brainStored > 0f,
            };
        }

        public static DefibrillatorOutcome ApplyDefibrillation(
            BodyZone zone,
            IList<OrganState> organs,
            ZoneDamageState[] zones,
            out float burnApplied)
        {
            burnApplied = 0f;

            if (zone != BodyZone.Chest)
            {
                return DefibrillatorOutcome.WrongZone;
            }

            float brainFunction = OrganSimulation.GetStoredOrganFunction(organs, OrganType.Brain);
            if (brainFunction <= 0f)
            {
                return DefibrillatorOutcome.NoResponse;
            }

            float heartFunction = OrganSimulation.GetStoredOrganFunction(organs, OrganType.Heart);
            if (heartFunction <= 0f)
            {
                OrganSimulation.SetOrganFunction(organs, OrganType.Heart, HealthConstants.DefibrillatorHeartRestorePercent);
                return DefibrillatorOutcome.Success;
            }

            burnApplied = HealthConstants.DefibrillatorMisshockBurnDamage;
            int chestIndex = (int)BodyZone.Chest;
            ZoneDamageState chest = zones[chestIndex];
            chest.Burn += burnApplied;
            chest.Severity = ResolveZoneSeverity(chest.Brute, chest.Burn);
            zones[chestIndex] = chest;
            return DefibrillatorOutcome.UnnecessaryShock;
        }

        public static SystemicPools ApplyBloodTransfusion(SystemicPools pools, float bloodRestore)
        {
            return new SystemicPools
            {
                BloodVolumeRatio = Clamp01(pools.BloodVolumeRatio + bloodRestore),
                OxyDebt = pools.OxyDebt,
                ToxinConcentration = pools.ToxinConcentration,
            };
        }

        public static SystemicPools ApplyOxyRelief(SystemicPools pools, float oxyRelief)
        {
            return new SystemicPools
            {
                BloodVolumeRatio = pools.BloodVolumeRatio,
                OxyDebt = Math.Max(0f, pools.OxyDebt - oxyRelief),
                ToxinConcentration = pools.ToxinConcentration,
            };
        }

        public static SystemicPools ApplyOxyDebt(SystemicPools pools, float oxyDebtGain)
        {
            return new SystemicPools
            {
                BloodVolumeRatio = pools.BloodVolumeRatio,
                OxyDebt = Math.Max(0f, pools.OxyDebt + Math.Max(0f, oxyDebtGain)),
                ToxinConcentration = pools.ToxinConcentration,
            };
        }

        public static SystemicPools ApplyAntitoxin(SystemicPools pools, float toxinReduction)
        {
            return new SystemicPools
            {
                BloodVolumeRatio = pools.BloodVolumeRatio,
                OxyDebt = pools.OxyDebt,
                ToxinConcentration = Math.Max(0f, pools.ToxinConcentration - toxinReduction),
            };
        }

        private static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
    }
}
