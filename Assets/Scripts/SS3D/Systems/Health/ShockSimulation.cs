using System;
using System.Collections.Generic;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Whole-body shock on a human (health.md §4). <see cref="IncomingPain"/> is pain from hits that has
    /// not yet turned into <see cref="Shock"/>; <see cref="BuildPerTick"/> spreads it over
    /// <see cref="HealthConstants.ShockBuildSeconds"/> health ticks after the last hit.
    /// </summary>
    [Serializable]
    public struct ShockState
    {
        public float Shock;
        public float IncomingPain;
        public float BuildPerTick;

        public static ShockState None => default;
    }

    /// <summary>
    /// Pure shock math — testable without Unity/FishNet. Hits add incoming pain, the 1 Hz health tick turns it
    /// into shock and drains shock towards a floor set by open wounds and lost blood.
    /// </summary>
    public static class ShockSimulation
    {
        public static float ZoneWeight(BodyZone zone)
        {
            return zone switch
            {
                BodyZone.Chest or BodyZone.Groin => HealthConstants.ShockTorsoZoneWeight,
                BodyZone.Head => HealthConstants.ShockHeadZoneWeight,
                _ => HealthConstants.ShockLimbZoneWeight,
            };
        }

        public static bool IsLimb(BodyZone zone)
        {
            return zone is BodyZone.LeftArm or BodyZone.RightArm or BodyZone.LeftLeg or BodyZone.RightLeg;
        }

        public static float TierSpike(WoundSeverity tier)
        {
            return tier switch
            {
                WoundSeverity.Bruised => HealthConstants.ShockSpikeBruised,
                WoundSeverity.Wound => HealthConstants.ShockSpikeWound,
                WoundSeverity.Severe => HealthConstants.ShockSpikeSevere,
                WoundSeverity.Disabled => HealthConstants.ShockSpikeDisabled,
                WoundSeverity.Severed => HealthConstants.ShockSpikeSevered,
                _ => 0f,
            };
        }

        /// <summary>The spikes for every tier a zone crossed going from <paramref name="before"/> to <paramref name="after"/>.</summary>
        public static float TierSpikes(BodyZone zone, WoundSeverity before, WoundSeverity after)
        {
            float spike = 0f;
            for (int tier = (int)before + 1; tier <= (int)after; tier++)
            {
                spike += TierSpike((WoundSeverity)tier);
            }

            if (zone == BodyZone.Head && before < WoundSeverity.Severe && after >= WoundSeverity.Severe)
            {
                spike += HealthConstants.ShockHeadSevereSpike;
            }

            return IsLimb(zone) ? spike * HealthConstants.ShockLimbSpikeScale : spike;
        }

        /// <summary>
        /// Incoming pain from one hit: damage that got through counts in full, a share of what armor absorbed still
        /// counts, and every wound tier the zone crossed adds its spike.
        /// </summary>
        public static float HitPain(
            BodyZone zone,
            float brute,
            float burn,
            float absorbedBrute,
            float absorbedBurn,
            WoundSeverity before,
            WoundSeverity after)
        {
            float weight = ZoneWeight(zone);
            float through = Math.Max(0f, brute) + (Math.Max(0f, burn) * HealthConstants.ShockBurnTypeWeight);
            float absorbed = Math.Max(0f, absorbedBrute) + (Math.Max(0f, absorbedBurn) * HealthConstants.ShockBurnTypeWeight);
            return (through * weight)
                + (absorbed * HealthConstants.ArmorShockFraction * weight)
                + TierSpikes(zone, before, after);
        }

        /// <summary>Adds pain and re-spreads everything still pending over the next build window.</summary>
        public static ShockState AddPain(ShockState state, float pain)
        {
            if (pain <= 0f)
            {
                return state;
            }

            state.IncomingPain += pain;
            state.BuildPerTick = state.IncomingPain / Math.Max(1f, HealthConstants.ShockBuildSeconds / HealthConstants.TickIntervalSeconds);
            return state;
        }

        /// <summary>
        /// The least shock a body can settle at: each open, untreated wound holds some (a bandaged zone holds none),
        /// and lost blood holds more.
        /// </summary>
        public static float Floor(IReadOnlyList<ZoneDamageState> zones, float bloodVolumeRatio)
        {
            float floor = 0f;
            for (int i = 0; i < zones.Count; i++)
            {
                ZoneDamageState zone = zones[i];
                if (zone.BleedingRate <= 0f)
                {
                    continue;
                }

                floor += zone.Severity switch
                {
                    WoundSeverity.Wound => HealthConstants.ShockFloorWound,
                    WoundSeverity.Severe => HealthConstants.ShockFloorSevere,
                    WoundSeverity.Disabled => HealthConstants.ShockFloorDisabled,
                    WoundSeverity.Severed => HealthConstants.ShockFloorSevered,
                    _ => 0f,
                };
            }

            return floor + (Math.Clamp(1f - bloodVolumeRatio, 0f, 1f) * HealthConstants.LowBloodShockScale);
        }

        /// <summary>
        /// One 1 Hz step: pending pain turns into shock; with none pending, shock moves towards the floor by the
        /// decay rate (down when it is above, up when lost blood has raised the floor past it).
        /// </summary>
        public static ShockState Tick(ShockState state, float floor)
        {
            if (state.IncomingPain > 0f)
            {
                float converted = Math.Min(state.IncomingPain, state.BuildPerTick > 0f ? state.BuildPerTick : state.IncomingPain);
                state.IncomingPain -= converted;
                state.Shock += converted;
                if (state.IncomingPain <= 1e-4f)
                {
                    state.IncomingPain = 0f;
                    state.BuildPerTick = 0f;
                }

                return state;
            }

            float step = HealthConstants.ShockDecayPerSecond * HealthConstants.TickIntervalSeconds;
            state.Shock = state.Shock > floor
                ? Math.Max(floor, state.Shock - step)
                : Math.Min(floor, state.Shock + step);
            return state;
        }

        /// <summary>A splinted leg holds weight; a severed one never does.</summary>
        public static bool IsUsableLeg(ZoneDamageState leg)
        {
            return !OrganSimulation.IsLimbFunctionallyDisabled(leg);
        }

        public static bool HasUsableLeg(IReadOnlyList<ZoneDamageState> zones)
        {
            if (zones == null || zones.Count < HealthConstants.ZoneCount)
            {
                return true;
            }

            return IsUsableLeg(zones[(int)BodyZone.LeftLeg]) || IsUsableLeg(zones[(int)BodyZone.RightLeg]);
        }

        public static bool IsCritical(float shock) => shock >= HealthConstants.ShockCriticalThreshold;

        /// <summary>
        /// Downed from shock or from having no leg to stand on. A body already down stays down until shock falls
        /// below the lower stand threshold, so it doesn't flicker at the line.
        /// </summary>
        public static bool IsDowned(float shock, bool hasUsableLeg, bool wasDowned)
        {
            if (!hasUsableLeg)
            {
                return true;
            }

            return shock >= (wasDowned ? HealthConstants.ShockStandThreshold : HealthConstants.ShockDownedThreshold);
        }

        /// <summary>
        /// Someone else may help a Downed body up once its shock is back under the Downed line and a leg can hold it
        /// (a splinted one counts). Critical bodies are not Downed, so they never qualify.
        /// </summary>
        public static bool CanBeHelpedUp(HealthSnapshot snapshot)
        {
            return snapshot.State == HealthState.Downed
                && snapshot.Shock < HealthConstants.ShockDownedThreshold
                && (!snapshot.IsZoneDisabled(BodyZone.LeftLeg) || !snapshot.IsZoneDisabled(BodyZone.RightLeg));
        }

        /// <summary>Helping up drops shock to just under the stand line; pain still pending keeps coming.</summary>
        public static ShockState HelpUp(ShockState state)
        {
            state.Shock = Math.Min(state.Shock, HealthConstants.ShockStandThreshold - 0.01f);
            return state;
        }

        /// <summary>Stamina cap scale (stamina.md §2): 1 with no shock, <see cref="HealthConstants.ShockStaminaCapFloor"/> at Downed.</summary>
        public static float StaminaCapScale(float shock)
        {
            float t = Math.Clamp(shock / HealthConstants.ShockDownedThreshold, 0f, 1f);
            return 1f + ((HealthConstants.ShockStaminaCapFloor - 1f) * t);
        }
    }
}
