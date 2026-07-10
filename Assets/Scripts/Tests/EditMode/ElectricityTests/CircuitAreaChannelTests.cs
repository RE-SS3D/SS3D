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
    }
}
