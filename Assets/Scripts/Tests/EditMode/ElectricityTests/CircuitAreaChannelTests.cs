using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using SS3D.Systems.Electricity;
using UnityEngine;

namespace EditorTests
{
    public class CircuitAreaChannelTests
    {
        private const float TestTickSeconds = 3600f;
        private const float Tolerance = 0.001f;

        [Test]
        public void AreaScopedConsumer_UsesResolverChannelsInsteadOfCircuitWideOr()
        {
            TestApcChannelSource circuitApc = new TestApcChannelSource(ApcControlFlags.Lighting);
            BasicPowerGenerator generator = CreateBasicGenerator(10f);
            BasicPowerConsumer equipmentConsumer = CreateBasicConsumer(1f, PowerChannel.Equipment);

            Circuit circuit = CreateCircuit(circuitApc, generator, equipmentConsumer);
            circuit.SetConsumerChannelResolver(_ => ApcControlFlags.Equipment);

            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.AreEqual(PowerStatus.Powered, equipmentConsumer.PowerStatus);
        }

        [Test]
        public void ConsumerWithoutAreaResolver_UsesCircuitWideChannelOr()
        {
            TestApcChannelSource circuitApc = new TestApcChannelSource(ApcControlFlags.Lighting);
            BasicPowerGenerator generator = CreateBasicGenerator(10f);
            BasicPowerConsumer equipmentConsumer = CreateBasicConsumer(1f, PowerChannel.Equipment);

            Circuit circuit = CreateCircuit(circuitApc, generator, equipmentConsumer);

            circuit.UpdateCircuitPower(TestTickSeconds);

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

            circuit.UpdateCircuitPower(TestTickSeconds);

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
            TestApcCell apcCell = new TestApcCell(storedEnergyKwh: 1f, maxCapacityKwh: 5f, maxDischargeRateKw: 5f);
            BasicPowerGenerator generator = CreateBasicGenerator(10f);
            BasicPowerConsumer equipmentConsumer = CreateBasicConsumer(1f, PowerChannel.Equipment);

            Circuit circuit = CreateCircuit(apcCell, generator, equipmentConsumer);
            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.That(apcCell.StoredEnergyKwh, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void UpdateCableDistributionOnly_DoesNotSetPowerStatusOnExcludedConsumers()
        {
            BasicPowerGenerator generator = CreateBasicGenerator(10f);
            BasicPowerConsumer cableConsumer = CreateBasicConsumer(1f, PowerChannel.Equipment);
            BasicPowerConsumer areaScopedConsumer = CreateBasicConsumer(1f, PowerChannel.Environment);
            areaScopedConsumer.PowerStatus = PowerStatus.Powered;

            Circuit circuit = CreateCircuit(generator, cableConsumer, areaScopedConsumer);
            circuit.SetCableDistributionFilter(consumer => consumer == cableConsumer);

            circuit.UpdateCableDistributionOnly(TestTickSeconds);

            Assert.AreEqual(PowerStatus.Powered, cableConsumer.PowerStatus);
            Assert.AreEqual(PowerStatus.Powered, areaScopedConsumer.PowerStatus);
        }

        [Test]
        public void GetAvailableGridSupplyForArea_IncludesStorageWhenCableDemandIsZero()
        {
            TestApcCell apcCell = new TestApcCell(storedEnergyKwh: 5f, maxCapacityKwh: 5f, maxDischargeRateKw: 5f);
            BasicBattery smes = CreateBasicBattery(5f, 50f, 50f);

            Circuit circuit = CreateCircuit(apcCell, smes);
            circuit.SetCableDistributionFilter(_ => false);
            circuit.UpdateCableDistributionOnly(TestTickSeconds);

            Assert.That(circuit.GetAvailableGridSupplyForArea(TestTickSeconds), Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void GetAvailableGridSupplyForArea_IncludesProducerSurplusAfterCableDemand()
        {
            BasicPowerGenerator generator = CreateBasicGenerator(10f);
            BasicPowerConsumer cableConsumer = CreateBasicConsumer(3f, PowerChannel.Equipment);

            Circuit circuit = CreateCircuit(generator, cableConsumer);
            circuit.UpdateCableDistributionOnly(TestTickSeconds);

            Assert.That(circuit.GetAvailableGridSupplyForArea(TestTickSeconds), Is.EqualTo(7f).Within(Tolerance));
        }

        [Test]
        public void DrawGridPowerForArea_DrainsNonApcStorageBeforeApcCellWouldBeUsed()
        {
            TestApcCell apcCell = new TestApcCell(storedEnergyKwh: 5f, maxCapacityKwh: 5f, maxDischargeRateKw: 5f);
            BasicBattery smes = CreateBasicBattery(5f, 50f, 50f);

            Circuit circuit = CreateCircuit(apcCell, smes);
            circuit.SetCableDistributionFilter(_ => false);
            circuit.UpdateCableDistributionOnly(TestTickSeconds);
            circuit.DrawGridPowerForArea(2f, TestTickSeconds);

            Assert.That(smes.StoredEnergyKwh, Is.EqualTo(48f).Within(Tolerance));
            Assert.That(apcCell.StoredEnergyKwh, Is.EqualTo(5f).Within(Tolerance));
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

        private static BasicBattery CreateBasicBattery(float maxDischargeRateKw, float maxCapacityKwh, float storedEnergyKwh)
        {
            GameObject batteryGo = new GameObject();
            batteryGo.AddComponent<BasicBattery>();
            batteryGo.AddComponent<PlacedTileObject>();
            BasicBattery battery = batteryGo.GetComponent<BasicBattery>();
            battery.Init(maxDischargeRateKw, maxCapacityKwh, storedEnergyKwh);
            battery.IsOn = true;
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
            public TestApcCell(float storedEnergyKwh, float maxCapacityKwh, float maxDischargeRateKw)
            {
                StoredEnergyKwh = storedEnergyKwh;
                _maxCapacityKwh = maxCapacityKwh;
                _maxDischargeRateKw = maxDischargeRateKw;
            }

            private readonly float _maxCapacityKwh;
            private readonly float _maxDischargeRateKw;

            public ApcControlFlags Channels => ApcControlFlags.All;

            public float StoredEnergyKwh { get; set; }

            public float MaxCapacityKwh => _maxCapacityKwh;

            public float RemainingCapacityKwh => _maxCapacityKwh - StoredEnergyKwh;

            public float MaxDischargeRateKw => _maxDischargeRateKw;

            public float MaxChargeRateKw => _maxDischargeRateKw;

            public float MaxDeliverableKw(float tickSeconds) =>
                Mathf.Min(_maxDischargeRateKw, ElectricityUnits.KwhToKw(StoredEnergyKwh, tickSeconds));

            public bool IsOn => true;

            public PlacedTileObject TileObject => null;

            public float AddPowerKw(float requestedKw, float tickSeconds)
            {
                float absorbedKw = Mathf.Min(requestedKw, _maxDischargeRateKw);
                float addedEnergy = Mathf.Min(RemainingCapacityKwh, ElectricityUnits.KwToKwh(absorbedKw, tickSeconds));
                StoredEnergyKwh += addedEnergy;
                return ElectricityUnits.KwhToKw(addedEnergy, tickSeconds);
            }

            public float RemovePowerKw(float requestedKw, float tickSeconds)
            {
                float deliveredKw = Mathf.Min(requestedKw, MaxDeliverableKw(tickSeconds));
                StoredEnergyKwh -= ElectricityUnits.KwToKwh(deliveredKw, tickSeconds);
                return deliveredKw;
            }
        }
    }
}
