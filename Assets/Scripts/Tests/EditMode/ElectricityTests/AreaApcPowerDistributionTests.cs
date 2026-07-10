using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Electricity;

namespace EditorTests
{
    public class AreaApcPowerDistributionTests
    {
        [Test]
        public void PowerAreaConsumers_GridCoversDemand_PowersAllConsumers()
        {
            TestConsumer consumer = new TestConsumer(5f, PowerChannel.Lighting);
            TestApcStorage apc = new TestApcStorage(storedPower: 5f, maxCapacity: 5f, maxPowerRate: 5f);

            AreaApcPowerDistribution.PowerAreaConsumers(
                apc,
                apc,
                gridSupplyKw: 10f,
                new[] { consumer },
                new[] { consumer });

            Assert.AreEqual(PowerStatus.Powered, consumer.PowerStatus);
            Assert.AreEqual(5f, apc.StoredPower);
        }

        [Test]
        public void PowerAreaConsumers_GridDeficit_DrainsApcCell()
        {
            TestConsumer consumer = new TestConsumer(8f, PowerChannel.Lighting);
            TestApcStorage apc = new TestApcStorage(storedPower: 5f, maxCapacity: 5f, maxPowerRate: 5f);

            AreaApcPowerDistribution.PowerAreaConsumers(
                apc,
                apc,
                gridSupplyKw: 0f,
                new[] { consumer },
                new[] { consumer });

            Assert.AreEqual(PowerStatus.Powered, consumer.PowerStatus);
            Assert.Less(apc.StoredPower, 5f);
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
            public TestApcStorage(float storedPower, float maxCapacity, float maxPowerRate)
            {
                StoredPower = storedPower;
                _maxCapacity = maxCapacity;
                _maxPowerRate = maxPowerRate;
            }

            private readonly float _maxCapacity;
            private readonly float _maxPowerRate;

            public ApcControlFlags Channels => ApcControlFlags.All;

            public float StoredPower { get; set; }

            public float MaxCapacity => _maxCapacity;

            public float RemainingCapacity => _maxCapacity - StoredPower;

            public float MaxPowerRate => _maxPowerRate;

            public float MaxRemovablePower => StoredPower < _maxPowerRate ? StoredPower : _maxPowerRate;

            public bool IsOn => true;

            public PlacedTileObject TileObject => null;

            public float AddPower(float amount)
            {
                float added = amount > RemainingCapacity ? RemainingCapacity : amount;
                StoredPower += added;
                return added;
            }

            public float RemovePower(float amount)
            {
                float removed = amount > StoredPower ? StoredPower : amount;
                StoredPower -= removed;
                return removed;
            }
        }
    }
}
