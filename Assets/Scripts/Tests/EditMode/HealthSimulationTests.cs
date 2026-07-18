using NUnit.Framework;
using SS3D.Systems.Health;
using SS3D.Systems.Stamina;
using System.Collections.Generic;

namespace EditorTests
{
    public class HealthSimulationTests
    {
        [Test]
        public void LowBloodVolumeRaisesOxyDebtEvenWithHealthyOrgans()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            zones[(int)BodyZone.Chest] = new ZoneDamageState
            {
                Brute = HealthConstants.WoundThreshold,
                Severity = WoundSeverity.Wound,
                BleedingRate = 0.5f,
            };

            var organs = new[]
            {
                OrganState.Default(OrganType.Heart),
                OrganState.Default(OrganType.LeftLung),
                OrganState.Default(OrganType.RightLung),
                OrganState.Default(OrganType.Liver),
                OrganState.Default(OrganType.Brain),
            };

            SystemicPools pools = SystemicPools.Default;

            // Wound bleed (0.5 × BleedingBloodDrainScale) needs ~45 ticks before LowBloodOxyDebtGainScale
            // overcomes healthy lung/heart balance (see HealthConstants hemorrhage tuning).
            for (int tick = 0; tick < 60; tick++)
            {
                pools = HealthSimulation.TickPools(pools, zones, organs);
            }

            Assert.Less(pools.BloodVolumeRatio, 1f);
            Assert.Greater(pools.OxyDebt, 0f);
        }

        [Test]
        public void OxyDebtRisesBeforeBloodReachesHalfVolume()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            zones[(int)BodyZone.Chest] = new ZoneDamageState
            {
                Brute = HealthConstants.SevereThreshold,
                Severity = WoundSeverity.Severe,
                BleedingRate = 1f,
            };

            var organs = new[]
            {
                OrganState.Default(OrganType.Heart),
                OrganState.Default(OrganType.LeftLung),
                OrganState.Default(OrganType.RightLung),
                OrganState.Default(OrganType.Liver),
                OrganState.Default(OrganType.Brain),
            };

            SystemicPools pools = SystemicPools.Default;
            bool oxyRoseEarly = false;

            // Severe bleed reaches ~70% blood (~hypoxia onset) by ~30 ticks at current drain scale.
            for (int tick = 0; tick < 35; tick++)
            {
                pools = HealthSimulation.TickPools(pools, zones, organs);
                if (pools.BloodVolumeRatio > 0.5f && pools.OxyDebt > 0f)
                {
                    oxyRoseEarly = true;
                    break;
                }
            }

            Assert.IsTrue(oxyRoseEarly);
        }

        [Test]
        public void HemorrhageCascadeKeepsOxyAfterBloodAndUsableDefibWindow()
        {
            // Wound-tier limb bleed: oxy critical after blood critical; arrest → death ≥ 15 s.
            AssertHemorrhageCascadeOrder(
                bleedingRate: HealthSimulation.BleedingRateForSeverity(WoundSeverity.Wound),
                brainStartPercent: 100f,
                minDefibWindowTicks: 15);

            AssertHemorrhageCascadeOrder(
                bleedingRate: HealthSimulation.BleedingRateForSeverity(WoundSeverity.Disabled),
                brainStartPercent: 100f,
                minDefibWindowTicks: 15);
        }

        private static void AssertHemorrhageCascadeOrder(float bleedingRate, float brainStartPercent, int minDefibWindowTicks)
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            zones[(int)BodyZone.LeftArm] = new ZoneDamageState
            {
                Severity = WoundSeverity.Wound,
                BleedingRate = bleedingRate,
            };

            var organs = new List<OrganState>
            {
                OrganState.Default(OrganType.Heart),
                OrganState.Default(OrganType.LeftLung),
                OrganState.Default(OrganType.RightLung),
                OrganState.Default(OrganType.Liver),
                new OrganState { Type = OrganType.Brain, FunctionPercent = brainStartPercent },
            };

            SystemicPools pools = SystemicPools.Default;
            int? bloodCriticalTick = null;
            int? oxyCriticalTick = null;
            int? arrestTick = null;
            int? deathTick = null;

            for (int tick = 1; tick <= 400; tick++)
            {
                pools = HealthSimulation.TickPools(pools, zones, organs);
                OrganSimulation.TickOrganFunction(pools, organs);

                if (bloodCriticalTick == null && pools.BloodVolumeRatio <= HealthConstants.CriticalBloodVolumeRatio)
                {
                    bloodCriticalTick = tick;
                }

                if (oxyCriticalTick == null && pools.OxyDebt >= HealthConstants.CriticalOxyDebt)
                {
                    oxyCriticalTick = tick;
                }

                float heart = OrganSimulation.GetStoredOrganFunction(organs, OrganType.Heart);
                float brain = OrganSimulation.GetStoredOrganFunction(organs, OrganType.Brain);

                if (arrestTick == null && heart <= 0f)
                {
                    arrestTick = tick;
                }

                if (brain <= 0f)
                {
                    deathTick = tick;
                    break;
                }
            }

            Assert.IsNotNull(bloodCriticalTick, "Never reached critical blood.");
            Assert.IsNotNull(oxyCriticalTick, "Never reached critical oxy debt.");
            Assert.IsNotNull(arrestTick, "Never reached cardiac arrest.");
            Assert.IsNotNull(deathTick, "Never reached brain death.");
            Assert.GreaterOrEqual(oxyCriticalTick.Value, bloodCriticalTick.Value,
                "Oxy critical should not outrun blood critical after bleed/oxy sync.");
            Assert.GreaterOrEqual(deathTick.Value - arrestTick.Value, minDefibWindowTicks,
                "Arrest → death defib window too short.");
        }

        [Test]
        public void UntreatedBleedTimelinesMatchHemorrhageTuningTargets()
        {
            AssertBleedTimeline(
                HealthSimulation.BleedingRateForSeverity(WoundSeverity.Wound),
                criticalTicks: 120,
                emptyTicks: 200,
                tickTolerance: 5);

            AssertBleedTimeline(
                HealthSimulation.BleedingRateForSeverity(WoundSeverity.Severe),
                criticalTicks: 60,
                emptyTicks: 100,
                tickTolerance: 5);

            AssertBleedTimeline(
                HealthSimulation.BleedingRateForSeverity(WoundSeverity.Disabled),
                criticalTicks: 40,
                emptyTicks: 67,
                tickTolerance: 5);
        }

        private static void AssertBleedTimeline(float bleedingRate, int criticalTicks, int emptyTicks, int tickTolerance)
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            zones[(int)BodyZone.LeftArm] = new ZoneDamageState
            {
                Severity = WoundSeverity.Wound,
                BleedingRate = bleedingRate,
            };

            var organs = new[]
            {
                OrganState.Default(OrganType.Heart),
                OrganState.Default(OrganType.LeftLung),
                OrganState.Default(OrganType.RightLung),
                OrganState.Default(OrganType.Liver),
                OrganState.Default(OrganType.Brain),
            };

            SystemicPools pools = SystemicPools.Default;
            int? ticksToCritical = null;
            int? ticksToEmpty = null;
            int maxTicks = emptyTicks + tickTolerance + 10;

            for (int tick = 1; tick <= maxTicks; tick++)
            {
                pools = HealthSimulation.TickPools(pools, zones, organs);

                if (ticksToCritical == null && pools.BloodVolumeRatio <= HealthConstants.CriticalBloodVolumeRatio)
                {
                    ticksToCritical = tick;
                }

                if (ticksToEmpty == null && pools.BloodVolumeRatio <= 0.001f)
                {
                    ticksToEmpty = tick;
                    break;
                }
            }

            Assert.IsNotNull(ticksToCritical, $"Bleed rate {bleedingRate} never reached critical blood.");
            Assert.IsNotNull(ticksToEmpty, $"Bleed rate {bleedingRate} never reached empty blood.");
            Assert.That(ticksToCritical.Value, Is.InRange(criticalTicks - tickTolerance, criticalTicks + tickTolerance));
            Assert.That(ticksToEmpty.Value, Is.InRange(emptyTicks - tickTolerance, emptyTicks + tickTolerance));
        }

        [Test]
        public void BrainFunctionZeroIsOnlyDeathTrigger()
        {
            var pools = SystemicPools.Default;
            var organs = new[]
            {
                OrganState.Default(OrganType.Heart),
                OrganState.Default(OrganType.LeftLung),
                OrganState.Default(OrganType.RightLung),
                OrganState.Default(OrganType.Liver),
                new OrganState { Type = OrganType.Brain, FunctionPercent = 0f },
            };

            Assert.AreEqual(HealthState.Dead, HealthSimulation.EvaluateHealthState(pools, organs));
        }

        [Test]
        public void CardiacArrestIsDistinctFromDeath()
        {
            var pools = SystemicPools.Default;
            var organs = new[]
            {
                new OrganState { Type = OrganType.Heart, FunctionPercent = 0f },
                OrganState.Default(OrganType.LeftLung),
                OrganState.Default(OrganType.RightLung),
                OrganState.Default(OrganType.Liver),
                OrganState.Default(OrganType.Brain),
            };

            Assert.AreEqual(HealthState.CardiacArrest, HealthSimulation.EvaluateHealthState(pools, organs));
        }

        [Test]
        public void StoppedBleedingHaltsBloodDrain()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            zones[(int)BodyZone.Chest] = new ZoneDamageState
            {
                Brute = HealthConstants.WoundThreshold,
                Severity = WoundSeverity.Wound,
                BleedingRate = 0.5f,
            };

            var organs = new[]
            {
                OrganState.Default(OrganType.Heart),
                OrganState.Default(OrganType.LeftLung),
                OrganState.Default(OrganType.RightLung),
                OrganState.Default(OrganType.Liver),
                OrganState.Default(OrganType.Brain),
            };

            SystemicPools bleedingPools = SystemicPools.Default;
            for (int tick = 0; tick < 5; tick++)
            {
                bleedingPools = HealthSimulation.TickPools(bleedingPools, zones, organs);
            }

            zones[(int)BodyZone.Chest] = new ZoneDamageState
            {
                Brute = HealthConstants.WoundThreshold,
                Severity = WoundSeverity.Wound,
                BleedingRate = 0f,
            };

            SystemicPools bandagedPools = bleedingPools;
            for (int tick = 0; tick < 5; tick++)
            {
                bandagedPools = HealthSimulation.TickPools(bandagedPools, zones, organs);
            }

            Assert.Less(bleedingPools.BloodVolumeRatio, 1f);
            Assert.AreEqual(bleedingPools.BloodVolumeRatio, bandagedPools.BloodVolumeRatio);
        }

        [Test]
        public void BuildSnapshotSetsBleedingZoneMask()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            zones[(int)BodyZone.LeftArm] = new ZoneDamageState
            {
                Brute = HealthConstants.WoundThreshold,
                Severity = WoundSeverity.Wound,
                BleedingRate = 0.5f,
            };

            var organs = new[] { OrganState.Default(OrganType.Brain) };
            HealthSnapshot snapshot = HealthSimulation.BuildSnapshot(SystemicPools.Default, zones, organs);

            Assert.IsTrue(snapshot.IsBleeding);
            Assert.IsTrue(snapshot.IsZoneBleeding(BodyZone.LeftArm));
            Assert.IsFalse(snapshot.IsZoneBleeding(BodyZone.Chest));
            Assert.AreEqual(0.5f, snapshot.GetZoneBleedingRate(BodyZone.LeftArm), 0.001f);
            Assert.AreEqual(0f, snapshot.GetZoneBleedingRate(BodyZone.Chest), 0.001f);
            Assert.AreEqual(0.5f, snapshot.TotalBleedingRate, 0.001f);
        }

        [Test]
        public void BuildSnapshotPacksMultipleZoneBleedRates()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            zones[(int)BodyZone.Head] = new ZoneDamageState
            {
                Severity = WoundSeverity.Wound,
                BleedingRate = 0.5f,
            };
            zones[(int)BodyZone.Chest] = new ZoneDamageState
            {
                Severity = WoundSeverity.Disabled,
                BleedingRate = 1.5f,
            };
            zones[(int)BodyZone.RightLeg] = new ZoneDamageState
            {
                Severity = WoundSeverity.Severed,
                IsSevered = true,
                BleedingRate = 2f,
            };

            HealthSnapshot snapshot = HealthSimulation.BuildSnapshot(
                SystemicPools.Default,
                zones,
                new[] { OrganState.Default(OrganType.Brain) });

            Assert.AreEqual(0.5f, snapshot.GetZoneBleedingRate(BodyZone.Head), 0.001f);
            Assert.AreEqual(1.5f, snapshot.GetZoneBleedingRate(BodyZone.Chest), 0.001f);
            Assert.AreEqual(2f, snapshot.GetZoneBleedingRate(BodyZone.RightLeg), 0.001f);
            Assert.AreEqual(4f, snapshot.TotalBleedingRate, 0.001f);
        }

        [Test]
        public void BurnDamageCanReachWoundSeverity()
        {
            WoundSeverity severity = HealthSimulation.ResolveZoneSeverity(0f, HealthConstants.BurnWoundThreshold);
            Assert.AreEqual(WoundSeverity.Wound, severity);
            Assert.AreEqual(0.5f, HealthSimulation.BleedingRateForSeverity(severity));
        }

        [Test]
        public void HeadDamageReducesBrainFunction()
        {
            var organs = new List<OrganState>
            {
                OrganState.Default(OrganType.Brain),
            };

            OrganSimulation.ApplyZoneDamageToOrgans(BodyZone.Head, 50f, 0f, organs);

            Assert.Less(organs[0].FunctionPercent, 100f);
        }

        [Test]
        public void CardiacArrestDrainsBrainOverTicks()
        {
            var organs = new List<OrganState>
            {
                new OrganState { Type = OrganType.Heart, FunctionPercent = 0f },
                OrganState.Default(OrganType.Brain),
            };

            var pools = SystemicPools.Default;
            OrganSimulation.TickOrganFunction(pools, organs);

            Assert.Less(organs[1].FunctionPercent, 100f);
        }

        [Test]
        public void DisabledLegReducesMovementMultiplier()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            zones[(int)BodyZone.LeftLeg] = new ZoneDamageState
            {
                Severity = WoundSeverity.Disabled,
                IsDisabled = true,
            };

            float multiplier = OrganSimulation.ComputeMovementSpeedMultiplier(zones);
            Assert.Less(multiplier, 1f);
        }

        [Test]
        public void LowBloodReducesEffectiveOrganFunction()
        {
            float effective = OrganSimulation.EffectiveOrganFunction(100f, 0.2f);
            Assert.Less(effective, 100f);
        }

        [Test]
        public void AnySystemicThresholdCanTriggerCritical()
        {
            var pools = new SystemicPools
            {
                BloodVolumeRatio = 0.2f,
                OxyDebt = 0f,
                ToxinConcentration = 0f,
            };

            var organs = new[] { OrganState.Default(OrganType.Brain) };

            Assert.AreEqual(HealthState.Critical, HealthSimulation.EvaluateHealthState(pools, organs));
        }

        [Test]
        public void CriticalOxyDebtDrainsHeartOverTicks()
        {
            var organs = new List<OrganState>
            {
                OrganState.Default(OrganType.Heart),
                OrganState.Default(OrganType.Brain),
            };

            var pools = new SystemicPools
            {
                BloodVolumeRatio = 1f,
                OxyDebt = HealthConstants.CriticalOxyDebt,
                ToxinConcentration = 0f,
            };

            OrganSimulation.TickOrganFunction(pools, organs);

            Assert.Less(OrganSimulation.GetStoredOrganFunction(organs, OrganType.Heart), 100f);
        }

        [Test]
        public void BuildSnapshotSetsCriticalFlagsAndDefibrillateEligibility()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            var organs = new[]
            {
                new OrganState { Type = OrganType.Heart, FunctionPercent = 0f },
                OrganState.Default(OrganType.Brain),
            };

            var pools = new SystemicPools
            {
                BloodVolumeRatio = 0.2f,
                OxyDebt = HealthConstants.CriticalOxyDebt,
                ToxinConcentration = 0f,
            };

            HealthSnapshot snapshot = HealthSimulation.BuildSnapshot(pools, zones, organs);

            Assert.AreEqual(HealthState.CardiacArrest, snapshot.State);
            Assert.IsTrue(snapshot.CanDefibrillate);
            Assert.IsTrue((snapshot.CriticalFlags & HealthCriticalFlags.LowBlood) != 0);
            Assert.IsTrue((snapshot.CriticalFlags & HealthCriticalFlags.HighOxyDebt) != 0);
        }

        [Test]
        public void DefibrillationRestoresHeartBeforeBrainDeath()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            var organs = new List<OrganState>
            {
                new OrganState { Type = OrganType.Heart, FunctionPercent = 0f },
                OrganState.Default(OrganType.Brain),
            };

            DefibrillatorOutcome outcome = HealthSimulation.ApplyDefibrillation(
                BodyZone.Chest,
                organs,
                zones,
                out float burnApplied);

            Assert.AreEqual(DefibrillatorOutcome.Success, outcome);
            Assert.AreEqual(0f, burnApplied);
            Assert.AreEqual(HealthConstants.DefibrillatorHeartRestorePercent, OrganSimulation.GetStoredOrganFunction(organs, OrganType.Heart));
        }

        [Test]
        public void DefibrillationHasNoResponseAfterBrainDeath()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            var organs = new List<OrganState>
            {
                new OrganState { Type = OrganType.Heart, FunctionPercent = 0f },
                new OrganState { Type = OrganType.Brain, FunctionPercent = 0f },
            };

            DefibrillatorOutcome outcome = HealthSimulation.ApplyDefibrillation(
                BodyZone.Chest,
                organs,
                zones,
                out _);

            Assert.AreEqual(DefibrillatorOutcome.NoResponse, outcome);
        }

        [Test]
        public void DefibrillationMisshockAppliesChestBurn()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            var organs = new List<OrganState>
            {
                OrganState.Default(OrganType.Heart),
                OrganState.Default(OrganType.Brain),
            };

            DefibrillatorOutcome outcome = HealthSimulation.ApplyDefibrillation(
                BodyZone.Chest,
                organs,
                zones,
                out float burnApplied);

            Assert.AreEqual(DefibrillatorOutcome.UnnecessaryShock, outcome);
            Assert.AreEqual(HealthConstants.DefibrillatorMisshockBurnDamage, burnApplied);
            Assert.AreEqual(HealthConstants.DefibrillatorMisshockBurnDamage, zones[(int)BodyZone.Chest].Burn);
        }

        [Test]
        public void BloodTransfusionRestoresBloodVolume()
        {
            SystemicPools pools = new SystemicPools
            {
                BloodVolumeRatio = 0.4f,
                OxyDebt = 0.5f,
                ToxinConcentration = 0.2f,
            };

            pools = HealthSimulation.ApplyBloodTransfusion(pools, HealthConstants.TransfusionBloodRestore);

            Assert.AreEqual(0.75f, pools.BloodVolumeRatio, 0.001f);
            Assert.AreEqual(0.5f, pools.OxyDebt, 0.001f);
        }

        [Test]
        public void OxyReliefAndAntitoxinReduceSystemicPools()
        {
            SystemicPools pools = new SystemicPools
            {
                BloodVolumeRatio = 0.6f,
                OxyDebt = 0.8f,
                ToxinConcentration = 0.9f,
            };

            pools = HealthSimulation.ApplyOxyRelief(pools, HealthConstants.OxygenTankOxyRelief);
            pools = HealthSimulation.ApplyAntitoxin(pools, HealthConstants.AntitoxinReduction);

            Assert.AreEqual(0.45f, pools.OxyDebt, 0.001f);
            Assert.AreEqual(0.5f, pools.ToxinConcentration, 0.001f);
        }

        [Test]
        public void SplintedDisabledLimbUsesPartialMovementMultiplier()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            zones[(int)BodyZone.LeftLeg] = new ZoneDamageState
            {
                Brute = HealthConstants.DisabledThreshold,
                Severity = WoundSeverity.Disabled,
                IsDisabled = true,
                IsSplinted = true,
            };

            float movement = OrganSimulation.ComputeMovementSpeedMultiplier(zones);
            Assert.AreEqual(HealthConstants.LimbSevereMovementMultiplier, movement, 0.001f);
            Assert.IsTrue(OrganSimulation.CanUseArms(zones));
        }

        [Test]
        public void ApplySeveranceSetsMaxBleedingAndPreservesStateOnRefresh()
        {
            ZoneDamageState state = ZoneDamageState.Default;
            HealthSimulation.ApplySeverance(ref state);

            Assert.IsTrue(state.IsSevered);
            Assert.AreEqual(WoundSeverity.Severed, state.Severity);
            Assert.AreEqual(2f, state.BleedingRate, 0.001f);

            state.Brute = 0f;
            HealthSimulation.RefreshZoneDerivedState(ref state);

            Assert.IsTrue(state.IsSevered);
            Assert.AreEqual(WoundSeverity.Severed, state.Severity);
        }

        [Test]
        public void SeveredLimbIsFunctionallyDisabledEvenWhenSplinted()
        {
            ZoneDamageState state = new ZoneDamageState
            {
                IsSevered = true,
                Severity = WoundSeverity.Severed,
                IsDisabled = true,
                IsSplinted = true,
            };

            Assert.IsTrue(OrganSimulation.IsLimbFunctionallyDisabled(state));
            Assert.AreEqual(HealthConstants.LimbDisabledMovementMultiplier,
                OrganSimulation.ComputeMovementSpeedMultiplier(new[]
                {
                    ZoneDamageState.Default,
                    ZoneDamageState.Default,
                    ZoneDamageState.Default,
                    ZoneDamageState.Default,
                    state,
                    ZoneDamageState.Default,
                }), 0.001f);
        }

        [Test]
        public void BuildSnapshotIncludesSeveredZoneMask()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            HealthSimulation.ApplySeverance(ref zones[(int)BodyZone.LeftArm]);
            var organs = new List<OrganState> { OrganState.Default(OrganType.Brain) };

            HealthSnapshot snapshot = HealthSimulation.BuildSnapshot(SystemicPools.Default, zones, organs);

            Assert.IsTrue(snapshot.IsZoneSevered(BodyZone.LeftArm));
            Assert.IsFalse(snapshot.IsZoneSevered(BodyZone.RightArm));
        }
    }
}
