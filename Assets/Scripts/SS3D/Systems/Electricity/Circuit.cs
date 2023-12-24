using SS3D.Utils;
using System.Collections.Generic;
using System.Linq;

namespace System.Electricity
{
    /// <summary>
    /// Class to store all connected consumers, producers and storages of electric power.
    /// It handles distributing power to all of those.
    /// </summary>
    public class Circuit
    {
        private List<IPowerConsumer> _consumers;
        private List<IPowerProducer> _producers;
        private List<IPowerStorage> _storages;
        private static Random RandomGenerator = new();

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
            float leftOverPower = ConsumePower(out bool enoughPower, out int firstUnpoweredConsumerIndex);
            ChargeStorages(leftOverPower);
            UpdateElectricElementStatus(!enoughPower, firstUnpoweredConsumerIndex);
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
                {
                    availablePower -= consumer.PowerNeeded;
                }
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
        /// <param name="firstUnpoweredConsumerByStoragesIndex"> Index of the first consumer in the consumer list
        /// that could not be powered by power storages because there's not enough power. Equals to 0 if there's enough power.</param>
        /// <returns> True if enough power is present to power all left over consumers, false otherwise.</returns>
        private bool ConsumePowerFromBatteries(IPowerConsumer firstUnPoweredConsumer, float leftOverPowerFromProducer,
             out int firstUnpoweredConsumerByStoragesIndex)
        {
            firstUnpoweredConsumerByStoragesIndex = 0;

            // We care only about the consumer that could not be alimented by producers.
            int firstUnpoweredConsumerByProducersIndex = _consumers.FindIndex(x => x == firstUnPoweredConsumer);

            List<IPowerStorage> storagesWithAvailablePower = _storages.Where(x => x.StoredPower > 0).ToList();

            // Consume power from batteries, starting from the first unpowered device by producers
            for (int i = firstUnpoweredConsumerByProducersIndex; i < _consumers.Count; i++)
            {
                IPowerConsumer consumer = _consumers[i];
                float powerNeeded = consumer.PowerNeeded;

                if (!AccumulateBatteryPower(powerNeeded, storagesWithAvailablePower, i == firstUnpoweredConsumerByProducersIndex ? leftOverPowerFromProducer : 0))
                {
                    firstUnpoweredConsumerByStoragesIndex = i;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Make the consumers fulfill all their need of power, from producers and storages.
        /// </summary>
        /// <returns> Excess produced energy </returns>
        private float ConsumePower(out bool enoughPower, out int firstUnPoweredConsumerIndex)
        {
            // Reorder randomly consumers so that if there's not enough power, powered consumers will be different with each tick.
            _consumers = _consumers.OrderBy(x => RandomGenerator.Next()).ToList();
            firstUnPoweredConsumerIndex = 0;
            float producedPower = _producers.Sum(x => x.PowerProduction);
            producedPower = ConsumePowerFromPowerProducingDevices(producedPower, out IPowerConsumer firstUnPoweredConsumer);

            // if the power producing device were enough to cover all needs just return.
            // Else continue with using power from batteries. 
            if (firstUnPoweredConsumer == null)
            {
                enoughPower = true;
                return producedPower;
            }

            enoughPower = ConsumePowerFromBatteries(firstUnPoweredConsumer, producedPower, out firstUnPoweredConsumerIndex);
            return 0;
        }

        /// <summary>
        /// Distribute available power equally to power storages.
        /// </summary>
        /// <param name="availablePower"> Power available after consuming.</param>
        /// <returns> Left over power if storages are full.</returns>
        private float ChargeStorages(float availablePower)
        {
            if (availablePower <= 0f) return 0f;

            List<IPowerStorage> notFullStorages = _storages.Where(x => x.RemainingCapacity > 0 && x.IsOn).ToList();

            while (availablePower > 0f && notFullStorages.Count > 0)
            {
                // compute an equal amount to all which are not full and are on.
                float equalAmount = availablePower / notFullStorages.Count;

                // Fill the storages that can be filled
                List<IPowerStorage> fullyFillableStorages = notFullStorages.Where(x => x.RemainingCapacity <= equalAmount).ToList();

                foreach (IPowerStorage storage in fullyFillableStorages)
                {
                    if (availablePower >= storage.RemainingCapacity)
                    {
                        availablePower -= storage.AddPower(availablePower);
                    }

                    notFullStorages.Remove(storage);

                    if (availablePower <= 0) break;
                }
            }

            // Those are the storage which can't be fully filled, so they are partially filled equally.
            if(availablePower > 0f && notFullStorages.Count > 0)
            {
                float equalAmount = availablePower / notFullStorages.Count;
                notFullStorages.ForEach(x => x.AddPower(equalAmount));
                availablePower -= equalAmount * notFullStorages.Count;
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
                else
                {
                    _consumers[i].PowerStatus = PowerStatus.Powered;
                }
            }
        }

        /// <summary>
        /// Given an amount of needed power, go through all the storages with available power and accumulate energy from them, until
        /// it reaches the necessary amount, or fail to do so. The power is picked up in random order for batteries, to avoid batteries
        /// draining one by one. This ensures an almost equal distribution of power taken between batteries.
        /// </summary>
        /// <param name="powerNeeded"> The amount of power we're trying to reach.</param>
        /// <param name="storagesWithAvailablePower"> The list of storages with power left.</param>
        /// <param name="leftOverPowerFromProducers"> Power left from producers, that could not go into any consumers.</param>
        /// <returns> True if enough power was accumulated, false otherwise.</returns>
        private bool AccumulateBatteryPower(float powerNeeded, List<IPowerStorage> storagesWithAvailablePower, float leftOverPowerFromProducers)
        {
            float powerFromStorages = leftOverPowerFromProducers;

            while (powerFromStorages < powerNeeded)
            {
                if (storagesWithAvailablePower.IsNullOrEmpty())
                {
                    return false;
                }

                // Pick a random battery to draw power from.
                int randomBatteryIndex = RandomGenerator.Next(0, storagesWithAvailablePower.Count);

                if (powerNeeded - powerFromStorages < storagesWithAvailablePower[randomBatteryIndex].MaxRemovablePower)
                {
                    powerFromStorages += storagesWithAvailablePower[randomBatteryIndex].RemovePower(powerNeeded - powerFromStorages);
                }
                else
                {
                    powerFromStorages += storagesWithAvailablePower[randomBatteryIndex].RemovePower(storagesWithAvailablePower[randomBatteryIndex].MaxRemovablePower);
                }

                // Can't take from a battery more than once each tick.
                storagesWithAvailablePower.RemoveAt(randomBatteryIndex);
            }

            return true;
        }
    }
}
