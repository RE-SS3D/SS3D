using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Electricity;

namespace EditorTests
{
    public class PowerGateTests
    {
        [Test]
        public void IsPowered_Null_Allow_ReturnsTrue()
        {
            Assert.IsTrue(PowerGate.IsPowered(null, NullConsumerPolicy.Allow));
        }

        [Test]
        public void IsPowered_Null_Deny_ReturnsFalse()
        {
            Assert.IsFalse(PowerGate.IsPowered(null, NullConsumerPolicy.Deny));
        }

        [Test]
        public void IsPowered_PoweredStatus_ReturnsTrue()
        {
            TestConsumer consumer = new TestConsumer(PowerStatus.Powered);
            Assert.IsTrue(PowerGate.IsPowered(consumer, NullConsumerPolicy.Deny));
        }

        [Test]
        public void IsPowered_InactiveStatus_ReturnsFalse()
        {
            TestConsumer consumer = new TestConsumer(PowerStatus.Inactive);
            Assert.IsFalse(PowerGate.IsPowered(consumer, NullConsumerPolicy.Allow));
        }

        [Test]
        public void IsChannelEnabled_MatchesChannelFlags()
        {
            Assert.IsTrue(PowerGate.IsChannelEnabled(PowerChannel.Lighting, ApcControlFlags.Lighting));
            Assert.IsFalse(PowerGate.IsChannelEnabled(PowerChannel.Equipment, ApcControlFlags.Lighting));
            Assert.IsTrue(PowerGate.IsChannelEnabled(PowerChannel.Environment, ApcControlFlags.All));
        }

        [Test]
        public void IsChannelOpen_NullConsumer_PassthroughTrue()
        {
            Assert.IsTrue(PowerGate.IsChannelOpen(null));
        }

        [Test]
        public void IsEffectivelyPowered_Null_Deny_ReturnsFalse()
        {
            Assert.IsFalse(PowerGate.IsEffectivelyPowered(null, NullConsumerPolicy.Deny));
        }

        [Test]
        public void IsEffectivelyPowered_PoweredWithoutArea_ReturnsTrue()
        {
            // Without AreaSubSystem, IsChannelOpen passthroughs to true.
            TestConsumer consumer = new TestConsumer(PowerStatus.Powered);
            Assert.IsTrue(PowerGate.IsEffectivelyPowered(consumer, NullConsumerPolicy.Deny));
        }

        private sealed class TestConsumer : IPowerConsumer
        {
            public TestConsumer(PowerStatus status)
            {
                PowerStatus = status;
            }

            public float PowerNeeded => 1f;

            public PowerChannel Channel => PowerChannel.Equipment;

            public PowerStatus PowerStatus { get; set; }

            public PlacedTileObject TileObject => null;
        }
    }
}
