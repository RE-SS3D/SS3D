using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Electricity;

namespace EditorTests
{
    public class AreaApcPowerDistributionTests
    {
        private const float TestTickSeconds = 3600f;
        private const float Tolerance = 0.0001f;

        [Test]
        public void PowerAreaConsumers_GridCoversDemand_PowersAllConsumers()
        {
            TestConsumer consumer = new TestConsumer(5f, PowerChannel.Lighting);
            TestApcStorage apc = new TestApcStorage(storedEnergyKwh: 5f, maxCapacityKwh: 5f, maxDischargeRateKw: 5f);

            AreaApcPowerDistribution.PowerAreaConsumers(
                apc,
                apc,
                gridSupplyKw: 10f,
                new[] { consumer },
                new[] { consumer },
                TestTickSeconds);

            Assert.AreEqual(PowerStatus.Powered, consumer.PowerStatus);
            Assert.That(apc.StoredEnergyKwh, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void PowerAreaConsumers_GridDeficit_DrainsApcCell()
        {
            // Ensure the APC cell can fully cover the consumer demand so the test asserts a drain, not load shedding.
            TestConsumer consumer = new TestConsumer(4f, PowerChannel.Lighting);
            TestApcStorage apc = new TestApcStorage(storedEnergyKwh: 5f, maxCapacityKwh: 5f, maxDischargeRateKw: 5f);

            AreaApcPowerDistribution.PowerAreaConsumers(
                apc,
                apc,
                gridSupplyKw: 0f,
                new[] { consumer },
                new[] { consumer },
                TestTickSeconds);

            Assert.AreEqual(PowerStatus.Powered, consumer.PowerStatus);
            Assert.That(apc.StoredEnergyKwh, Is.LessThan(5f));
        }

        [Test]
        public void PowerAreaConsumers_GridDeficit_ShedsEquipmentBeforeLighting()
        {
            TestConsumer lighting = new TestConsumer(2f, PowerChannel.Lighting);
            TestConsumer equipment = new TestConsumer(2f, PowerChannel.Equipment);
            TestApcStorage apc = new TestApcStorage(storedEnergyKwh: 5f, maxCapacityKwh: 5f, maxDischargeRateKw: 2f);

            AreaApcPowerDistribution.PowerAreaConsumers(
                apc,
                apc,
                gridSupplyKw: 0f,
                new[] { lighting, equipment },
                new[] { lighting, equipment },
                TestTickSeconds);

            Assert.AreEqual(PowerStatus.Powered, lighting.PowerStatus);
            Assert.AreEqual(PowerStatus.Inactive, equipment.PowerStatus);
            Assert.That(apc.StoredEnergyKwh, Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void PowerAreaConsumers_PartialBudget_RestoresLightingBeforeEquipment()
        {
            TestConsumer lighting = new TestConsumer(2f, PowerChannel.Lighting);
            TestConsumer equipment = new TestConsumer(2f, PowerChannel.Equipment);
            // No cell contribution: only grid supply is available for this tick.
            TestApcStorage apc = new TestApcStorage(storedEnergyKwh: 0f, maxCapacityKwh: 5f, maxDischargeRateKw: 0f);

            AreaApcPowerDistribution.PowerAreaConsumers(
                apc,
                apc,
                gridSupplyKw: 2f,
                new[] { lighting, equipment },
                new[] { lighting, equipment },
                TestTickSeconds);

            Assert.AreEqual(PowerStatus.Powered, lighting.PowerStatus);
            Assert.AreEqual(PowerStatus.Inactive, equipment.PowerStatus);
        }

        [Test]
        public void BuildApcStats_UsesAreaConsumerDemandOnly()
        {
            var stats = AreaApcPowerDistribution.BuildApcStats(
                gridSupplyKw: 10f,
                apcCell: new TestApcStorage(1f, 5f, 5f),
                new[] { new TestConsumer(2f, PowerChannel.Lighting), new TestConsumer(3f, PowerChannel.Equipment) });

            Assert.AreEqual(5f, stats.TotalDemandKw);
            Assert.AreEqual(2f, stats.LightingLoadKw);
            Assert.AreEqual(3f, stats.EquipmentLoadKw);
            Assert.IsTrue(stats.GridMeetsLoad);
        }

        private sealed class TestConsumer : IPowerConsumer
        {
            public TestConsumer(float powerNeeded, PowerChannel channel)
            {
                PowerNeeded = powerNeeded;
                Channel = channel;
            }

            public float PowerNeeded { get; }

            public PowerChannel Channel { get; }

            public PowerStatus PowerStatus { get; set; }

            public PlacedTileObject TileObject => null;
        }

        private sealed class TestApcStorage : IApcChannelSource, IPowerStorage
        {
            public TestApcStorage(float storedEnergyKwh, float maxCapacityKwh, float maxDischargeRateKw, float maxChargeRateKw = -1f)
            {
                StoredEnergyKwh = storedEnergyKwh;
                _maxCapacityKwh = maxCapacityKwh;
                _maxDischargeRateKw = maxDischargeRateKw;
                _maxChargeRateKw = maxChargeRateKw < 0f ? maxDischargeRateKw : maxChargeRateKw;
            }

            private readonly float _maxCapacityKwh;
            private readonly float _maxDischargeRateKw;
            private readonly float _maxChargeRateKw;

            public ApcControlFlags Channels => ApcControlFlags.All;

            public float StoredEnergyKwh { get; set; }

            public float MaxCapacityKwh => _maxCapacityKwh;

            public float RemainingCapacityKwh => _maxCapacityKwh - StoredEnergyKwh;

            public float MaxDischargeRateKw => _maxDischargeRateKw;

            public float MaxChargeRateKw => _maxChargeRateKw;

            public float MaxDeliverableKw(float tickSeconds)
            {
                if (StoredEnergyKwh <= 0f)
                {
                    return 0f;
                }

                return UnityEngine.Mathf.Min(_maxDischargeRateKw, ElectricityUnits.KwhToKw(StoredEnergyKwh, tickSeconds));
            }

            public bool IsOn => true;

            public PlacedTileObject TileObject => null;

            public float AddPowerKw(float requestedKw, float tickSeconds)
            {
                if (requestedKw <= 0f || RemainingCapacityKwh <= 0f)
                {
                    return 0f;
                }

                float absorbedKw = UnityEngine.Mathf.Min(requestedKw, _maxChargeRateKw);
                float energyToAdd = ElectricityUnits.KwToKwh(absorbedKw, tickSeconds);
                float addedEnergy = UnityEngine.Mathf.Min(RemainingCapacityKwh, energyToAdd);
                StoredEnergyKwh += addedEnergy;
                return ElectricityUnits.KwhToKw(addedEnergy, tickSeconds);
            }

            public float RemovePowerKw(float requestedKw, float tickSeconds)
            {
                if (requestedKw <= 0f || StoredEnergyKwh <= 0f)
                {
                    return 0f;
                }

                float deliverableKw = MaxDeliverableKw(tickSeconds);
                float deliveredKw = UnityEngine.Mathf.Min(requestedKw, deliverableKw);
                StoredEnergyKwh -= ElectricityUnits.KwToKwh(deliveredKw, tickSeconds);
                return deliveredKw;
            }
        }
    }
}
