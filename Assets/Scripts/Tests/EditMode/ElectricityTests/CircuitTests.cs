using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Electricity;
using System.Linq;
using UnityEngine;

namespace EditorTests
{
    public class CircuitTests
    {
        private const float Tolerance = 0.0001f;
        private const float TestTickSeconds = 3600f;

        [Test]
        public void BatteriesChargeAtEqualRatesOrGetFull()
        {
            BasicBattery batteryOne = CreateBasicBattery(5f, 50f, 0f);
            BasicBattery batteryTwo = CreateBasicBattery(5f, 50f, 45f);
            BasicBattery batteryThree = CreateBasicBattery(5f, 50f, 0f);
            BasicPowerGenerator generator = CreateBasicGenerator(9f);

            Circuit circuit = CreateCircuit(batteryOne, batteryTwo, batteryThree, generator);
            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.That(batteryOne.StoredEnergyKwh, Is.EqualTo(batteryThree.StoredEnergyKwh).Within(Tolerance));
            Assert.That(batteryOne.StoredEnergyKwh, Is.EqualTo(3f).Within(Tolerance));

            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.That(batteryOne.StoredEnergyKwh, Is.EqualTo(batteryThree.StoredEnergyKwh).Within(Tolerance));
            Assert.That(batteryOne.StoredEnergyKwh, Is.EqualTo(6.5f).Within(Tolerance));
        }

        [Test]
        public void BatteriesOffDontProvidePower()
        {
            BasicBattery batteryOne = CreateBasicBattery(5f, 50f, 50f);
            batteryOne.IsOn = false;
            BasicPowerConsumer consumerOne = CreateBasicConsumer(1f);

            Circuit circuit = CreateCircuit(batteryOne, consumerOne);
            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.That(batteryOne.StoredEnergyKwh, Is.EqualTo(50f).Within(Tolerance));
            Assert.AreEqual(PowerStatus.Inactive, consumerOne.PowerStatus);
        }

        [Test]
        public void BatteryCannotSendPowerAboveItsMaxRate()
        {
            BasicBattery batteryOne = CreateBasicBattery(5f, 50f, 50f);
            BasicPowerConsumer consumerOne = CreateBasicConsumer(15f);

            Circuit circuit = CreateCircuit(batteryOne, consumerOne);
            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.AreEqual(PowerStatus.Inactive, consumerOne.PowerStatus);
        }

        [Test]
        public void PowerGeneratedGoesFirstToConsumers()
        {
            BasicPowerGenerator generator = CreateBasicGenerator(5f);
            BasicPowerConsumer consumerOne = CreateBasicConsumer(2f);
            BasicPowerConsumer consumerTwo = CreateBasicConsumer(2f);
            BasicBattery batteryOne = CreateBasicBattery(5f, 50f, 0f);

            Circuit circuit = CreateCircuit(generator, consumerOne, consumerTwo, batteryOne);
            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.That(batteryOne.StoredEnergyKwh, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void TurnOffConsumersWhenNotEnoughPowerIsGenerated()
        {
            BasicPowerGenerator generator = CreateBasicGenerator(5f);
            BasicPowerConsumer consumerOne = CreateBasicConsumer(7f);
            BasicPowerConsumer consumerTwo = CreateBasicConsumer(2f);

            Circuit circuit = CreateCircuit(generator, consumerOne, consumerTwo);
            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.AreEqual(PowerStatus.Inactive, consumerOne.PowerStatus);
            Assert.AreEqual(PowerStatus.Powered, consumerTwo.PowerStatus);
        }

        [Test]
        public void BatteryLevelStayBetweenMaxAmountAndZero()
        {
            BasicBattery batteryOne = CreateBasicBattery(5f, 50f, 0f);
            batteryOne.AddPowerKw(500f, TestTickSeconds);
            Assert.That(batteryOne.StoredEnergyKwh, Is.EqualTo(50f).Within(Tolerance));
            batteryOne.RemovePowerKw(500f, TestTickSeconds);
            Assert.That(batteryOne.StoredEnergyKwh, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void AddingPowerToBatteryReturnsTheCorrectAmountOfPowerAdded()
        {
            BasicBattery batteryOne = CreateBasicBattery(50f, 50f, 0f);
            float added = batteryOne.AddPowerKw(47f, TestTickSeconds);
            Assert.That(added, Is.EqualTo(47f).Within(Tolerance));
            added = batteryOne.AddPowerKw(50f, TestTickSeconds);
            Assert.That(added, Is.EqualTo(3f).Within(Tolerance));
            added = batteryOne.AddPowerKw(50f, TestTickSeconds);
            Assert.That(added, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void RemovingPowerFromBatteryReturnsTheCorrectAmountOfPowerRemoved()
        {
            BasicBattery batteryOne = CreateBasicBattery(50f, 50f, 50f);
            float removed = batteryOne.RemovePowerKw(47f, TestTickSeconds);
            Assert.That(removed, Is.EqualTo(47f).Within(Tolerance));
            removed = batteryOne.RemovePowerKw(50f, TestTickSeconds);
            Assert.That(removed, Is.EqualTo(3f).Within(Tolerance));
            removed = batteryOne.RemovePowerKw(50f, TestTickSeconds);
            Assert.That(removed, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void BatteryMaxPowerRateIsUsedProperly()
        {
            BasicBattery batteryOne = CreateBasicBattery(3f, 50f, 50f);
            BasicPowerConsumer consumerOne = CreateBasicConsumer(2f);
            BasicPowerConsumer consumerTwo = CreateBasicConsumer(2f);

            Circuit circuit = CreateCircuit(batteryOne, consumerOne, consumerTwo);
            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.That(batteryOne.StoredEnergyKwh, Is.EqualTo(48f).Within(Tolerance));
            Assert.IsTrue(consumerOne.PowerStatus == PowerStatus.Powered ^ consumerTwo.PowerStatus == PowerStatus.Powered);
        }

        [Test]
        public void BatteriesSendPowerDespiteNotCoveringAllConsumersNeed()
        {
            BasicBattery batteryOne = CreateBasicBattery(5f, 50f, 50f);
            BasicBattery batteryTwo = CreateBasicBattery(5f, 50f, 50f);
            BasicPowerConsumer consumerOne = CreateBasicConsumer(2f);
            BasicPowerConsumer consumerTwo = CreateBasicConsumer(7f);

            Circuit circuit = CreateCircuit(batteryOne, batteryTwo, consumerOne, consumerTwo);
            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.That(batteryOne.StoredEnergyKwh, Is.LessThan(50f));
            Assert.That(batteryTwo.StoredEnergyKwh, Is.LessThan(50f));
        }

        [Test]
        public void BatteryCanPowerMultipleConsumersPerUpdate()
        {
            BasicBattery batteryOne = CreateBasicBattery(5f, 50f, 50f);
            BasicPowerConsumer consumerOne = CreateBasicConsumer(2f);
            BasicPowerConsumer consumerTwo = CreateBasicConsumer(2f);

            Circuit circuit = CreateCircuit(batteryOne, consumerOne, consumerTwo);
            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.That(batteryOne.StoredEnergyKwh, Is.EqualTo(46f).Within(Tolerance));
            Assert.AreEqual(PowerStatus.Powered, consumerOne.PowerStatus);
            Assert.AreEqual(PowerStatus.Powered, consumerTwo.PowerStatus);
        }

        [Test]
        public void BatteriesContributeEquallyToPowerConsumer()
        {
            BasicBattery batteryOne = CreateBasicBattery(5f, 50f, 50f);
            BasicBattery batteryTwo = CreateBasicBattery(2f, 50f, 50f);
            BasicPowerConsumer consumerOne = CreateBasicConsumer(4f);

            Circuit circuit = CreateCircuit(batteryOne, batteryTwo, consumerOne);
            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.That(batteryOne.StoredEnergyKwh, Is.EqualTo(48f).Within(Tolerance));
            Assert.That(batteryTwo.StoredEnergyKwh, Is.EqualTo(48f).Within(Tolerance));
        }

        [Test]
        public void PowerFromMultipleBatteriesIsFullyConsumedByAConsumer()
        {
            List<BasicBattery> batteries = new()
            {
                CreateBasicBattery(3f, 50f, 1f),
                CreateBasicBattery(5f, 50f, 15f),
                CreateBasicBattery(9f, 50f, 40f),
                CreateBasicBattery(2f, 50f, 30f),
                CreateBasicBattery(18f, 50f, 16f),
            };

            float firstSum = batteries.Sum(x => x.StoredEnergyKwh);
            BasicPowerConsumer consumerOne = CreateBasicConsumer(30f);
            List<IElectricDevice> electricDevices = new List<IElectricDevice> { consumerOne };
            electricDevices.AddRange(batteries);

            Circuit circuit = CreateCircuit(electricDevices.ToArray());
            circuit.UpdateCircuitPower(TestTickSeconds);
            float secondSum = batteries.Sum(x => x.StoredEnergyKwh);

            Assert.That(firstSum - secondSum, Is.EqualTo(consumerOne.PowerNeeded).Within(Tolerance));
        }

        [Test]
        public void CableConsumersShedEquipmentBeforeLighting()
        {
            BasicBattery battery = CreateBasicBattery(3f, 50f, 50f);
            BasicPowerConsumer lighting = CreateBasicConsumer(2f, PowerChannel.Lighting);
            BasicPowerConsumer equipment = CreateBasicConsumer(2f, PowerChannel.Equipment);

            Circuit circuit = CreateCircuit(battery, lighting, equipment);
            circuit.UpdateCircuitPower(TestTickSeconds);

            Assert.AreEqual(PowerStatus.Powered, lighting.PowerStatus);
            Assert.AreEqual(PowerStatus.Inactive, equipment.PowerStatus);
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

        private static BasicPowerConsumer CreateBasicConsumer(float powerConsumption, PowerChannel channel = PowerChannel.Equipment)
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
    }
}
