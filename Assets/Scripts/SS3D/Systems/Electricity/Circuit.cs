using SS3D.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace System.Electricity
{
    public class Circuit
    {
        private List<IPowerConsumer> _consumers;
        private List<IPowerProducer> _producers;
        private List<IPowerStorage> _storages;
       
        
        public List<IPowerConsumer> Consumers { get { return _consumers; } }

        public Circuit() 
        {
            _consumers = new();
            _producers = new();
            _storages = new();
        }

        
        public void AddConsumer(IPowerConsumer consumer)
        {
            _consumers.Add(consumer);
        }

        public void AddProducer(IPowerProducer producer)
        {
            _producers.Add(producer);
        }

        public void AddStorage(IPowerStorage storage)
        {
            _storages.Add(storage);
        }

        public void UpdateCircuitPower()
        {
            float leftOverPower = ConsumePower(out bool notEnoughPower, out int firstUnpoweredConsumerIndex);
            UpdateElectricElementStatus(notEnoughPower, firstUnpoweredConsumerIndex);
            leftOverPower = ChargeStorages(leftOverPower);
        }

        /// <summary>
        /// TODO : reorder randomly consumers, so that when there's not enough power, random electric objects get powered and unpowered.
        /// </summary>
        /// <returns> excess energy produced by producers</returns>
        private float ConsumePower(out bool notEnoughPower, out int firstUnPoweredConsumerByStoragesIndex)
        {
            notEnoughPower = false;
            firstUnPoweredConsumerByStoragesIndex = 0;

            float producedPower = _producers.Sum(x => x.PowerProduction);
            IPowerConsumer firstUnPoweredConsumer = null;

            // Consume power from the power producing devices.
            foreach (IPowerConsumer consumer in _consumers)
            {
                if(producedPower > consumer.PowerNeeded)
                    producedPower -= consumer.PowerNeeded;
                else
                {
                    firstUnPoweredConsumer = consumer;
                    break;
                }
            }

            // if the power producing device were enough to cover all needs just return.
            // Else continue with using power from batteries. 
            if (firstUnPoweredConsumer == null) return producedPower;

            // We care only about the consumer that could not be alimented by producers.
            int firstUnPoweredConsumerByProducersIndex = _consumers.FindIndex(x => x == firstUnPoweredConsumer);


            // Consume power from batteries
            for (int i = firstUnPoweredConsumerByProducersIndex; i < _consumers.Count; i++)
            {
                IPowerConsumer consumer = _consumers[i];
                float powerNeeded = consumer.PowerNeeded;
                float powerFromStorages = 0;
                float totalPowerLeftInStorages = _storages.Sum(x => x.StoredPower);

               

                if (totalPowerLeftInStorages < powerNeeded)
                {
                    firstUnPoweredConsumerByStoragesIndex = i;
                    notEnoughPower = true;
                    break;
                }
                

                List<IPowerStorage> storagesWithLeftPower = _storages.Where(x => x.StoredPower >= 0).ToList();

                while (powerFromStorages < powerNeeded)
                {
                    if (storagesWithLeftPower.IsNullOrEmpty())
                    {
                        notEnoughPower = true;
                        break;
                    }
                    if (storagesWithLeftPower[0].StoredPower > powerNeeded)
                    {
                        powerFromStorages = powerNeeded;
                        storagesWithLeftPower[0].StoredPower -= powerNeeded;
                        continue;
                    }
                    else
                    {
                        powerFromStorages += storagesWithLeftPower[0].StoredPower;
                        storagesWithLeftPower[0].StoredPower = 0;
                        storagesWithLeftPower.Remove(storagesWithLeftPower.First());
                    }
                }

                if (notEnoughPower)
                {
                    firstUnPoweredConsumerByStoragesIndex = i;
                    break;
                }
            }

            UpdateElectricElementStatus(notEnoughPower, firstUnPoweredConsumerByStoragesIndex);

            return 0f;
        }

        /// <summary>
        /// Distribute available power equally to power storages.
        /// </summary>
        /// <param name="availablePower"> Power available after consuming.</param>
        /// return : left over power if storages are full.
        private float ChargeStorages(float availablePower)
        {
            if (availablePower <= 0f) return 0f;


            float equalAmount = availablePower / _storages.Count;
            var notFullyFillableStorages = _storages.Where(x => x.RemainingCapacity > equalAmount).ToList();
            var fullyFillableStorages = _storages.Where(x => x.RemainingCapacity <= equalAmount).ToList();

            notFullyFillableStorages.ForEach(x => x.StoredPower += equalAmount);
            availablePower -= equalAmount * notFullyFillableStorages.Count;

            foreach(IPowerStorage storage in fullyFillableStorages)
            {
                if(availablePower >= storage.RemainingCapacity)
                {
                    storage.StoredPower += storage.RemainingCapacity;
                    availablePower -= storage.RemainingCapacity;
                }

                if (availablePower <= 0) break;
            }

            return availablePower;

        }

        private void UpdateElectricElementStatus(bool notEnoughPower, int firstUnpoweredIndex)
        {

            for(int i =0; i < _consumers.Count; i++)
            {
                if(notEnoughPower && i == firstUnpoweredIndex)
                {
                    _consumers[i].PowerStatus = PowerStatus.Inactive;
                }
                else
                {
                    _consumers[i].PowerStatus = PowerStatus.Powered;
                }
            }

        }
    }
}
