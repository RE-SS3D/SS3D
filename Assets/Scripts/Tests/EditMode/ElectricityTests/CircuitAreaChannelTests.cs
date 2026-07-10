using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;

namespace EditorTests
{
    public class CircuitAreaChannelTests
    {
        [Test]
        public void AreaScopedConsumer_UsesResolverChannelsInsteadOfCircuitWideOr()
        {
            TestApcChannelSource circuitApc = new TestApcChannelSource(ApcControlFlags.Lighting);
            BasicPowerGenerator generator = CreateBasicGenerator(10f);
            BasicPowerConsumer equipmentConsumer = CreateBasicConsumer(1f, PowerChannel.Equipment);

            Circuit circuit = CreateCircuit(circuitApc, generator, equipmentConsumer);
            circuit.SetConsumerChannelResolver(_ => ApcControlFlags.Equipment);

            circuit.UpdateCircuitPower();

            Assert.AreEqual(PowerStatus.Powered, equipmentConsumer.PowerStatus);
        }

        [Test]
        public void ConsumerWithoutAreaResolver_UsesCircuitWideChannelOr()
        {
            TestApcChannelSource circuitApc = new TestApcChannelSource(ApcControlFlags.Lighting);
            BasicPowerGenerator generator = CreateBasicGenerator(10f);
            BasicPowerConsumer equipmentConsumer = CreateBasicConsumer(1f, PowerChannel.Equipment);

            Circuit circuit = CreateCircuit(circuitApc, generator, equipmentConsumer);

            circuit.UpdateCircuitPower();

            Assert.AreEqual(PowerStatus.Inactive, equipmentConsumer.PowerStatus);
        }

        [Test]
        public void UnrelatedCircuitApc_DoesNotOverrideAreaScopedResolver()
        {
            TestApcChannelSource lightingOnlyApc = new TestApcChannelSource(ApcControlFlags.Lighting);
            TestApcChannelSource allChannelsApc = new TestApcChannelSource(ApcControlFlags.All);
            BasicPowerGenerator generator = CreateBasicGenerator(10f);
            BasicPowerConsumer equipmentConsumer = CreateBasicConsumer(1f, PowerChannel.Equipment);

            Circuit circuit = CreateCircuit(lightingOnlyApc, allChannelsApc, generator, equipmentConsumer);
            circuit.SetConsumerChannelResolver(_ => ApcControlFlags.Equipment);

            circuit.UpdateCircuitPower();

            Assert.AreEqual(PowerStatus.Powered, equipmentConsumer.PowerStatus);
        }

        [Test]
        public void GetStatsForConsumers_ScopesDemandToProvidedConsumers()
        {
            TestApcChannelSource apc = new TestApcChannelSource(ApcControlFlags.All);
            BasicPowerConsumer lightingConsumer = CreateBasicConsumer(2f, PowerChannel.Lighting);
            BasicPowerConsumer equipmentConsumer = CreateBasicConsumer(3f, PowerChannel.Equipment);

            Circuit circuit = CreateCircuit(apc, lightingConsumer, equipmentConsumer);

            CircuitStats scopedStats = circuit.GetStatsForConsumers(null, new[] { lightingConsumer });
            CircuitStats fullStats = circuit.GetStats(null);

            Assert.AreEqual(2f, scopedStats.TotalDemandKw);
            Assert.AreEqual(2f, scopedStats.LightingLoadKw);
            Assert.AreEqual(0f, scopedStats.EquipmentLoadKw);
            Assert.AreEqual(5f, fullStats.TotalDemandKw);
        }

        [Test]
        public void UpdateCircuitPower_DoesNotChargeApcCellStorage()
        {
            TestApcCell apcCell = new TestApcCell(storedPower: 1f, maxCapacity: 5f, maxPowerRate: 5f);
            BasicPowerGenerator generator = CreateBasicGenerator(10f);
            BasicPowerConsumer equipmentConsumer = CreateBasicConsumer(1f, PowerChannel.Equipment);

            Circuit circuit = CreateCircuit(apcCell, generator, equipmentConsumer);
            circuit.UpdateCircuitPower();

            Assert.AreEqual(1f, apcCell.StoredPower);
        }

        [Test]
        public void GetAvailableGridSupplyForArea_IncludesStorageWhenCableDemandIsZero()
        {
            TestApcCell apcCell = new TestApcCell(storedPower: 5f, maxCapacity: 5f, maxPowerRate: 5f);
            BasicBattery smes = CreateBasicBattery(5f, 50f, 50f);

            Circuit circuit = CreateCircuit(apcCell, smes);
            circuit.SetCableDistributionFilter(_ => false);
            circuit.UpdateCableDistributionOnly();

            Assert.AreEqual(5f, circuit.GetAvailableGridSupplyForArea(), 0.001f);
        }

        [Test]
        public void DrawGridPowerForArea_DrainsNonApcStorageBeforeApcCellWouldBeUsed()
        {
            TestApcCell apcCell = new TestApcCell(storedPower: 5f, maxCapacity: 5f, maxPowerRate: 5f);
            BasicBattery smes = CreateBasicBattery(5f, 50f, 50f);

            Circuit circuit = CreateCircuit(apcCell, smes);
            circuit.SetCableDistributionFilter(_ => false);
            circuit.UpdateCableDistributionOnly();
            circuit.DrawGridPowerForArea(2f);

            Assert.AreEqual(48f, smes.StoredPower, 0.001f);
            Assert.AreEqual(5f, apcCell.StoredPower, 0.001f);
        }

        private static Circuit CreateCircuit(params IElectricDevice[] electricDevices)
        {
            Circuit circuit = new Circuit();
            foreach (IElectricDevice device in electricDevices)
            {
                circuit.AddElectricDevice(device);
            }

            return circuit;
        }

        private static BasicPowerConsumer CreateBasicConsumer(float powerConsumption, PowerChannel channel)
        {
            GameObject consumerGo = new GameObject();
            consumerGo.AddComponent<BasicPowerConsumer>();
            consumerGo.AddComponent<PlacedTileObject>();
            BasicPowerConsumer consumer = consumerGo.GetComponent<BasicPowerConsumer>();
            consumer.Init(powerConsumption, channel);
            return consumer;
        }

        private static BasicPowerGenerator CreateBasicGenerator(float generatedPower)
        {
            GameObject generatorGo = new GameObject();
            generatorGo.AddComponent<BasicPowerGenerator>();
            generatorGo.AddComponent<PlacedTileObject>();
            BasicPowerGenerator generator = generatorGo.GetComponent<BasicPowerGenerator>();
            generator.PowerProduction = generatedPower;
            return generator;
        }

        private static BasicBattery CreateBasicBattery(float maxPowerRate, float maxCapacity, float storedPower)
        {
            GameObject batteryGo = new GameObject();
            batteryGo.AddComponent<BasicBattery>();
            batteryGo.AddComponent<PlacedTileObject>();
            BasicBattery battery = batteryGo.GetComponent<BasicBattery>();
            battery.Init(maxPowerRate, maxCapacity, storedPower);
            return battery;
        }

        private sealed class TestApcChannelSource : IApcChannelSource
        {
            private readonly PlacedTileObject _tileObject;

            public TestApcChannelSource(ApcControlFlags channels)
            {
                var gameObject = new GameObject();
                _tileObject = gameObject.AddComponent<PlacedTileObject>();
                Channels = channels;
            }

            public ApcControlFlags Channels { get; }

            public PlacedTileObject TileObject => _tileObject;
        }

        private sealed class TestApcCell : IApcChannelSource, IPowerStorage
        {
            public TestApcCell(float storedPower, float maxCapacity, float maxPowerRate)
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
