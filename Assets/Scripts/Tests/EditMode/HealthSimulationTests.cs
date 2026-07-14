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
    }
}
