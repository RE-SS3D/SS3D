using System;
using System.Collections.Generic;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Organ function math — zone-to-organ damage, perfusion, and limb capability.
    /// </summary>
    public static class OrganSimulation
    {
        public static void ApplyZoneDamageToOrgans(BodyZone zone, float brute, float burn, IList<OrganState> organs)
        {
            switch (zone)
            {
                case BodyZone.Head:
                    ApplyOrganDamage(organs, OrganType.Brain, brute * HealthConstants.HeadBruteToBrainDamageScale + burn * HealthConstants.HeadBurnToBrainDamageScale);
                    break;
                case BodyZone.Chest:
                case BodyZone.Groin:
                    ApplyOrganDamage(organs, OrganType.Heart, brute * HealthConstants.ChestBruteToHeartDamageScale);
                    ApplyOrganDamage(organs, OrganType.LeftLung, brute * HealthConstants.ChestBruteToLungDamageScale);
                    ApplyOrganDamage(organs, OrganType.RightLung, brute * HealthConstants.ChestBruteToLungDamageScale);
                    ApplyOrganDamage(organs, OrganType.Liver, brute * HealthConstants.ChestBruteToLiverDamageScale);
                    break;
            }
        }

        public static void TickOrganFunction(SystemicPools pools, IList<OrganState> organs)
        {
            if (pools.OxyDebt >= HealthConstants.CriticalOxyDebt)
            {
                ApplyOrganDamage(organs, OrganType.Heart, HealthConstants.CriticalOxyHeartDrainPerTick);
            }

            if (pools.BloodVolumeRatio <= HealthConstants.CriticalBloodVolumeRatio)
            {
                ApplyOrganDamage(organs, OrganType.Heart, HealthConstants.CriticalBloodHeartDrainPerTick);
            }

            if (pools.ToxinConcentration >= HealthConstants.CriticalToxinConcentration)
            {
                ApplyOrganDamage(organs, OrganType.Heart, HealthConstants.CriticalToxinHeartDrainPerTick);
            }

            float heartFunction = GetStoredOrganFunction(organs, OrganType.Heart);

            if (heartFunction <= 0f)
            {
                ApplyOrganDamage(organs, OrganType.Brain, HealthConstants.CardiacArrestBrainDrainPerTick);
            }

            if (pools.OxyDebt >= HealthConstants.CriticalOxyDebt)
            {
                ApplyOrganDamage(organs, OrganType.Brain, HealthConstants.CriticalOxyBrainDrainPerTick);
            }

            UpdateCriticalFlags(organs);
        }

        public static void SetOrganFunction(IList<OrganState> organs, OrganType type, float functionPercent)
        {
            functionPercent = Math.Clamp(functionPercent, 0f, 100f);

            for (int i = 0; i < organs.Count; i++)
            {
                if (organs[i].Type != type)
                {
                    continue;
                }

                OrganState organ = organs[i];
                organ.FunctionPercent = functionPercent;
                organs[i] = organ;
                UpdateCriticalFlags(organs);
                return;
            }

            organs.Add(new OrganState
            {
                Type = type,
                FunctionPercent = functionPercent,
                IsCritical = functionPercent <= HealthConstants.CriticalOrganFunctionPercent,
            });
        }

        public static float EffectiveOrganFunction(float storedFunctionPercent, float bloodVolumeRatio)
        {
            float perfusion = Math.Clamp(
                HealthConstants.OrganPerfusionBloodFloor + bloodVolumeRatio * (1f - HealthConstants.OrganPerfusionBloodFloor),
                HealthConstants.OrganPerfusionBloodFloor,
                1f);
            return storedFunctionPercent * perfusion;
        }

        public static float ComputeMovementSpeedMultiplier(IReadOnlyList<ZoneDamageState> zones)
        {
            float leftLeg = LimbMovementFactor(zones[(int)BodyZone.LeftLeg]);
            float rightLeg = LimbMovementFactor(zones[(int)BodyZone.RightLeg]);
            return Math.Min(leftLeg, rightLeg);
        }

        public static bool CanUseArms(IReadOnlyList<ZoneDamageState> zones)
        {
            bool leftDisabled = IsLimbFunctionallyDisabled(zones[(int)BodyZone.LeftArm]);
            bool rightDisabled = IsLimbFunctionallyDisabled(zones[(int)BodyZone.RightArm]);
            return !(leftDisabled && rightDisabled);
        }

        public static bool IsLimbZone(BodyZone zone)
        {
            return zone is BodyZone.LeftArm or BodyZone.RightArm or BodyZone.LeftLeg or BodyZone.RightLeg;
        }

        public static bool IsLimbFunctionallyDisabled(ZoneDamageState zone)
        {
            return zone.IsSevered || (zone.IsDisabled && !zone.IsSplinted);
        }

        public static float GetStoredOrganFunction(IEnumerable<OrganState> organs, OrganType type)
        {
            foreach (OrganState organ in organs)
            {
                if (organ.Type == type)
                {
                    return organ.FunctionPercent;
                }
            }

            return 100f;
        }

        public static void EnsureDefaultOrgans(IList<OrganState> organs)
        {
            EnsureOrgan(organs, OrganType.Brain);
            EnsureOrgan(organs, OrganType.Heart);
            EnsureOrgan(organs, OrganType.LeftLung);
            EnsureOrgan(organs, OrganType.RightLung);
            EnsureOrgan(organs, OrganType.Liver);
        }

        private static void EnsureOrgan(IList<OrganState> organs, OrganType type)
        {
            for (int i = 0; i < organs.Count; i++)
            {
                if (organs[i].Type == type)
                {
                    return;
                }
            }

            organs.Add(OrganState.Default(type));
        }

        private static void ApplyOrganDamage(IList<OrganState> organs, OrganType type, float damage)
        {
            if (damage <= 0f)
            {
                return;
            }

            for (int i = 0; i < organs.Count; i++)
            {
                if (organs[i].Type != type)
                {
                    continue;
                }

                OrganState organ = organs[i];
                organ.FunctionPercent = Math.Max(0f, organ.FunctionPercent - damage);
                organs[i] = organ;
                return;
            }

            organs.Add(new OrganState
            {
                Type = type,
                FunctionPercent = Math.Max(0f, 100f - damage),
                IsCritical = false,
            });
        }

        private static float LimbMovementFactor(ZoneDamageState zone)
        {
            if (zone.IsSevered || zone.IsDisabled)
            {
                return zone.IsSevered || !zone.IsSplinted
                    ? HealthConstants.LimbDisabledMovementMultiplier
                    : HealthConstants.LimbSevereMovementMultiplier;
            }

            if (zone.Severity >= WoundSeverity.Severe)
            {
                return HealthConstants.LimbSevereMovementMultiplier;
            }

            if (zone.Severity >= WoundSeverity.Wound)
            {
                return HealthConstants.LimbWoundMovementMultiplier;
            }

            return 1f;
        }

        private static void UpdateCriticalFlags(IList<OrganState> organs)
        {
            for (int i = 0; i < organs.Count; i++)
            {
                OrganState organ = organs[i];
                organ.IsCritical = organ.FunctionPercent <= HealthConstants.CriticalOrganFunctionPercent;
                organs[i] = organ;
            }
        }
    }
}
