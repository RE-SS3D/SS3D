using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Electricity;

namespace EditorTests
{
    public class PowerConsumerAllocationTests
    {
        [Test]
        public void AllocateUnderBudget_PrefersLightingOverEquipment()
        {
            var consumers = new List<IPowerConsumer>
            {
                new TestConsumer(2f, PowerChannel.Equipment),
                new TestConsumer(2f, PowerChannel.Lighting),
            };

            List<IPowerConsumer> powered = PowerConsumerAllocation.AllocateUnderBudget(consumers, 2f);

            Assert.AreEqual(1, powered.Count);
            Assert.AreEqual(PowerChannel.Lighting, powered[0].Channel);
        }

        [Test]
        public void AllocateUnderBudget_ShedsEquipmentBeforeEnvironmentAndLighting()
        {
            var consumers = new List<IPowerConsumer>
            {
                new TestConsumer(2f, PowerChannel.Equipment),
                new TestConsumer(2f, PowerChannel.Environment),
                new TestConsumer(2f, PowerChannel.Lighting),
            };

            List<IPowerConsumer> powered = PowerConsumerAllocation.AllocateUnderBudget(consumers, 4f);

            Assert.AreEqual(2, powered.Count);
            Assert.Contains(consumers[2], powered);
            Assert.Contains(consumers[1], powered);
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
    }
}
