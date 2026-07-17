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

            for (int tick = 0; tick < 20; tick++)
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

            for (int tick = 0; tick < 15; tick++)
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
