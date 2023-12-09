using SS3D.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;


namespace System.Electricity
{
    public class Circuit
    {
        private List<IPowerConsumer> _consumers;
        private List<IPowerProducer> _producers;
        private List<IPowerStorage> _storages;

        private static Random RandomGenerator = new Random();


        public List<IPowerConsumer> Consumers { get { return _consumers; } }

        public Circuit() 
        {
            _consumers = new();
            _producers = new();
            _storages = new();
        }

        /// <summary>
        /// Add an electric device to the circuit. An electric device can be a consumer and a producer, or
        /// a consumer and storage at the same time, or consumer, producer and storage. In this case, the electric device
        /// can end up in multiple lists simultaneously.
        /// </summary>
        public void AddElectricDevice(IElectricDevice device)
        {
            if(device is IPowerConsumer consumer) _consumers.Add(consumer);
            if (device is IPowerProducer producer) _producers.Add(producer);
            if (device is IPowerStorage storage) _storages.Add(storage);
        }

        
        /// <summary>
        /// Do an update on the whole circuit power. Produce power, consume power that needs to be consumed, and charge stuff that can be charged.
        /// </summary>
        public void UpdateCircuitPower()
        {
            float leftOverPower = ConsumePower(out bool notEnoughPower, out int firstUnpoweredConsumerIndex);
            UpdateElectricElementStatus(notEnoughPower, firstUnpoweredConsumerIndex);
            leftOverPower = ChargeStorages(leftOverPower);
        }

        /// <summary>
        /// Consume power from the producing devices.
        /// </summary>
        /// <param name="availablePower"> The available power, produced by producing devices</param>
        /// <param name="firstUnPoweredConsumer"> The first unpowered consumer, can be null if all consumers are powered.</param>
        /// <returns> The leftover power from the producing devices.</returns>
        private float ConsumePowerFromPowerProducingDevices(float availablePower, out IPowerConsumer firstUnPoweredConsumer)
        {
            firstUnPoweredConsumer = null;

            // Consume power from the power producing devices.
            foreach (IPowerConsumer consumer in _consumers)
            {
                if (availablePower > consumer.PowerNeeded)
                    availablePower -= consumer.PowerNeeded;
                else
                {
                    firstUnPoweredConsumer = consumer;
                    break;
                }
            }

            return availablePower;
        }

        /// <summary>
        /// Consume power from batteries if the producers don't produce enough power for all consumers.
        /// </summary>
        /// <param name="firstUnPoweredConsumer"> The first unpowered consumer in the list of consumers.</param>
        /// <param name="notEnoughPower"> Should be set to true if the storages don't have enough power for all unpowered consumers.</param>
        /// <param name="firstUnPoweredConsumerByStoragesIndex"> Should be set to be the index of the first consumer in the consumer list
        /// that could not be powered by power storages because there's not enough power. We don't care about it if there's enough power.</param>
        private void ConsumePowerFromBatteries(IPowerConsumer firstUnPoweredConsumer, float leftOverPowerFromProducer,
            out bool notEnoughPower, out int firstUnPoweredConsumerByStoragesIndex)
        {
            notEnoughPower = false;
            firstUnPoweredConsumerByStoragesIndex = 0;

            // We care only about the consumer that could not be alimented by producers.
            int firstUnPoweredConsumerByProducersIndex = _consumers.FindIndex(x => x == firstUnPoweredConsumer);

            List<IPowerStorage> storagesWithAvailablePower = _storages.Where(x => x.StoredPower >= 0).ToList();

            // Consume power from batteries, starting from the first unpowered device by producers
            for (int i = firstUnPoweredConsumerByProducersIndex; i < _consumers.Count; i++)
            {
                IPowerConsumer consumer = _consumers[i];
                float powerNeeded = consumer.PowerNeeded;
                float powerFromStorages = 0;

                // only call once, left over power from power producers to help the batteries
                if (i == firstUnPoweredConsumerByProducersIndex) powerFromStorages += leftOverPowerFromProducer;


                float totalPowerLeftInStorages = _storages.Sum(x => x.StoredPower);

                if (totalPowerLeftInStorages < powerNeeded)
                {
                    firstUnPoweredConsumerByStoragesIndex = i;
                    notEnoughPower = true;
                    break;
                }

                // Get power from randomly picked batteries until there's enough power for the current consumer.
                // Can go through a battery only once each update.
                while (powerFromStorages < powerNeeded)
                {
                    if (storagesWithAvailablePower.IsNullOrEmpty())
                    {
                        notEnoughPower = true;
                        break;
                    }

                    int randomBatteryIndex = RandomGenerator.Next(0, storagesWithAvailablePower.Count);

                    if(powerNeeded - powerFromStorages < storagesWithAvailablePower[randomBatteryIndex].MaxRemovablePower)
                    {
                        powerFromStorages += storagesWithAvailablePower[randomBatteryIndex].RemovePower(powerNeeded - powerFromStorages);
                    }
                    else
                    {
                        powerFromStorages += storagesWithAvailablePower[randomBatteryIndex].RemovePower(storagesWithAvailablePower[randomBatteryIndex].MaxRemovablePower);
                    }

                    storagesWithAvailablePower.RemoveAt(randomBatteryIndex);
                }

                if (notEnoughPower)
                {
                    firstUnPoweredConsumerByStoragesIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Make the consumers consume all their need of available power, from producers and storage, if they can.
        /// </summary>
        /// <returns> excess energy produced by producers</returns>
        private float ConsumePower(out bool notEnoughPower, out int firstUnPoweredConsumerIndex)
        {
            _consumers = _consumers.OrderBy(x => RandomGenerator.Next()).ToList();
            notEnoughPower = false;
            firstUnPoweredConsumerIndex = 0;

            float producedPower = _producers.Sum(x => x.PowerProduction);

            producedPower = ConsumePowerFromPowerProducingDevices(producedPower, out IPowerConsumer firstUnPoweredConsumer);

            // if the power producing device were enough to cover all needs just return.
            // Else continue with using power from batteries. 
            if (firstUnPoweredConsumer == null) return producedPower;

            ConsumePowerFromBatteries(firstUnPoweredConsumer, producedPower, out notEnoughPower, out firstUnPoweredConsumerIndex);

            UpdateElectricElementStatus(notEnoughPower, firstUnPoweredConsumerIndex);

            return 0f;
        }

        /// <summary>
        /// Distribute available power equally to power storages.
        /// </summary>
        /// <param name="availablePower"> Power available after consuming.</param>
        /// <returns> Left over power if storages are full.</returns>
        private float ChargeStorages(float availablePower)
        {
            if (availablePower <= 0f) return 0f;

            // distribute an equal amount to all which are not full and are on.
            float equalAmount = availablePower / _storages.Where(x => x.RemainingCapacity > 0 && x.IsOn).Count();
            var notFullyFillableStorages = _storages.Where(x => x.RemainingCapacity > equalAmount).ToList();
            var fullyFillableStorages = _storages.Where(x => x.RemainingCapacity <= equalAmount).ToList();

            notFullyFillableStorages.ForEach(x => x.AddPower(equalAmount));
            availablePower -= equalAmount * notFullyFillableStorages.Count;

            // fill the ones that can be filled
            foreach(IPowerStorage storage in fullyFillableStorages)
            {
                if (availablePower >= storage.RemainingCapacity)
                {
                    availablePower -= storage.AddPower(availablePower);
                }

                if (availablePower <= 0) break;
            }

            return availablePower;

        }

        /// <summary>
        /// Update the status of all consumers, if they're still powered or not.
        /// </summary>
        /// <param name="notEnoughPower"> true if all consumers could not be powered. </param>
        /// <param name="firstUnpoweredIndex"> The first consumer that could not be powered. We don't care about this if notEnoughPower is false.</param>

        private void UpdateElectricElementStatus(bool notEnoughPower, int firstUnpoweredIndex)
        {

            for(int i =0; i < _consumers.Count; i++)
            {
                if(notEnoughPower && i >= firstUnpoweredIndex)
                {
                    _consumers[i].PowerStatus = PowerStatus.Inactive;
                }
                else if(notEnoughPower && i < firstUnpoweredIndex)
                {
                    _consumers[i].PowerStatus = PowerStatus.Powered;
                }
                else
                {
                    _consumers[i].PowerStatus = PowerStatus.Powered;
                }
            }

        }

        
    }
}
