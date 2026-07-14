using NUnit.Framework;
using SS3D.Systems.Health;
using SS3D.Systems.Stamina;

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
