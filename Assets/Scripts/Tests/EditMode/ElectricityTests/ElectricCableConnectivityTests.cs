using NUnit.Framework;
using SS3D.Systems.Electricity;
using SS3D.Systems.Tile;
using SS3D.Systems.Electricity;

namespace EditorTests
{
    public class ElectricCableConnectivityTests
    {
        [Test]
        public void ParticipatesInCableGrid_IncludesProducersAndStorage()
        {
            Assert.IsTrue(ElectricCableConnectivity.ParticipatesInCableGrid(new TestProducer()));
            Assert.IsTrue(ElectricCableConnectivity.ParticipatesInCableGrid(new TestStorage()));
        }

        [Test]
        public void ParticipatesInCableGrid_ExcludesConsumers()
        {
            Assert.IsFalse(ElectricCableConnectivity.ParticipatesInCableGrid(new TestConsumer()));
        }

        private sealed class TestProducer : IPowerProducer
        {
            public float PowerProduction => 10f;

            public PlacedTileObject TileObject => null;
        }

        private sealed class TestStorage : IPowerStorage
        {
            public float StoredEnergyKwh => 1f;

            public float MaxCapacityKwh => 5f;

            public float RemainingCapacityKwh => 4f;

            public float MaxDischargeRateKw => 5f;

            public float MaxChargeRateKw => 5f;

            public bool IsOn => true;

            public PlacedTileObject TileObject => null;

            public float MaxDeliverableKw(float tickSeconds) => 5f;

            public float RemovePowerKw(float requestedKw, float tickSeconds) => 0f;

            public float AddPowerKw(float requestedKw, float tickSeconds) => 0f;
        }

        private sealed class TestConsumer : IPowerConsumer
        {
            public float PowerNeeded => 1f;

            public PowerChannel Channel => PowerChannel.Equipment;

            public PowerStatus PowerStatus { get; set; }

            public PlacedTileObject TileObject => null;
        }
    }
}
