using SS3D.Logging;
using System.Collections;
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
        private static Random RandomGenerator = new();

        private List<IPowerConsumer> _consumers;
        private List<IPowerProducer> _producers;
        private List<IPowerStorage> _storages;
        private List<IApcChannelSource> _apcChannelSources;

        public Circuit()
        {
            _consumers = new();
            _producers = new();
            _storages = new();
            _apcChannelSources = new();
        }

        /// <summary>
        /// Add an electric device to the circuit. An electric device can be a consumer and a producer, or
        /// a consumer and storage at the same time, or consumer, producer and storage. In this case, the electric device
        /// can end up in multiple lists simultaneously.
        /// </summary>
        public void AddElectricDevice(IElectricDevice device)
        {
            switch (device)
            {
                case IPowerConsumer consumer:
                    _consumers.Add(consumer);
                    break;

                case IPowerProducer producer:
                    _producers.Add(producer);
                    break;

                case IPowerStorage storage:
                    _storages.Add(storage);
                    break;
            }

            if (device is IApcChannelSource apcChannelSource)
            {
                _apcChannelSources.Add(apcChannelSource);
            }
        }

        public bool ContainsDevice(IElectricDevice device)
        {
            return device switch
            {
                IPowerConsumer consumer => _consumers.Contains(consumer),
                IPowerProducer producer => _producers.Contains(producer),
                IPowerStorage storage => _storages.Contains(storage),
                _ => _apcChannelSources.Contains(device as IApcChannelSource),
            };
        }

        public CircuitStats GetStats(IPowerStorage apcCell)
        {
            ApcControlFlags enabledChannels = GetEnabledChannels();
            List<IPowerConsumer> activeConsumers = GetActiveConsumers(enabledChannels);

            float supplyKw = _producers.Sum(x => x.PowerProduction);
            float demandKw = activeConsumers.Sum(x => x.PowerNeeded);
            float batteryCharge = apcCell != null && apcCell.MaxCapacity > 0f
                ? apcCell.StoredPower / apcCell.MaxCapacity
                : 0f;

            return new CircuitStats
            {
                TotalSupplyKw = supplyKw,
                TotalDemandKw = demandKw,
                ApcBatteryCharge = batteryCharge,
                LightingLoadKw = SumChannelLoad(activeConsumers, PowerChannel.Lighting),
                EquipmentLoadKw = SumChannelLoad(activeConsumers, PowerChannel.Equipment),
                EnvironmentLoadKw = SumChannelLoad(activeConsumers, PowerChannel.Environment),
                GridMeetsLoad = supplyKw >= demandKw,
                BatteryDraining = supplyKw < demandKw && apcCell is { StoredPower: > 0f, IsOn: true },
            };
        }

        /// <summary>
        /// Do an update on the whole circuit power. Produce power, consume power that needs to be consumed, and charge stuff that can be charged.
        /// </summary>
        public void UpdateCircuitPower()
        {
            ApcControlFlags enabledChannels = GetEnabledChannels();
            List<IPowerConsumer> activeConsumers = GetActiveConsumers(enabledChannels);
            float leftOverPower = ConsumePower(activeConsumers, out List<IPowerConsumer> poweredConsumers);
            ChargeStorages(leftOverPower);
            UpdateConsumerStatus(poweredConsumers);
        }

        /// <summary>
        /// Turn on or off consumers, depending on whether they are powered.
        /// </summary>
        /// <param name="poweredConsumers">Consumers, that were powered</param>
        private void UpdateConsumerStatus(List<IPowerConsumer> poweredConsumers)
        {
            foreach (IPowerConsumer consumer in _consumers)
            {
                if (poweredConsumers.Contains(consumer))
                {
                    consumer.PowerStatus = PowerStatus.Powered;
                }
                else
                {
                    consumer.PowerStatus = PowerStatus.Inactive;
                }
            }
        }

        /// <summary>
        /// Try to satisfy all consumers with power generators and drain storages for power if needed.
        /// </summary>
        /// <param name="poweredConsumers">Which consumers were satisfied</param>
        /// <returns>Power from generators, that wasn't used</returns>
        private float ConsumePower(List<IPowerConsumer> activeConsumers, out List<IPowerConsumer> poweredConsumers)
        {
            poweredConsumers = new();
            poweredConsumers.AddRange(activeConsumers);
            float powerFromProducers = _producers.Sum(x => x.PowerProduction);
            float neededPower = activeConsumers.Sum(x => x.PowerNeeded) - powerFromProducers;

            if (neededPower <= 0)
            {
                return -neededPower;
            }

            List<IPowerStorage> availableStorages = _storages.Where(x => x.IsOn && x.MaxRemovablePower > 0)
                .OrderBy(x => x.MaxRemovablePower).ToList();
            float maxPowerFromBatteries = availableStorages.Sum(x => x.MaxRemovablePower);

            while (neededPower > maxPowerFromBatteries)
            {
                List<IPowerConsumer> consumersToRemove = poweredConsumers.Where(x => x.PowerNeeded >= neededPower).ToList();
                if (!consumersToRemove.Any())
                {
                    consumersToRemove.AddRange(poweredConsumers);
                }

                // Random is used to make sure that unpowered consumers won't be the same each tick
                IPowerConsumer consumer = consumersToRemove[RandomGenerator.Next(consumersToRemove.Count)];
                neededPower -= consumer.PowerNeeded;
                poweredConsumers.Remove(consumer);
            }

            DrainBatteries(neededPower, availableStorages);
            return 0;
        }

        /// <summary>
        /// Drain batteries from storages equally
        /// </summary>
        /// <param name="power">Power to drain in sum. Must be always lesser or equal than sum of available power from storages</param>
        /// <param name="storages">Storages to drain from</param>
        private void DrainBatteries(float power, List<IPowerStorage> storages)
        {
            if (storages.Count == 0)
            {
                return;
            }

            if (power > storages.Sum(x => x.MaxRemovablePower))
            {
                Log.Error(this, "Energy requested for draining batteries is greater than available energy in batteries." +
                    "This will result in creating some free energy.");
            }

            float equalAmount = power / storages.Count;
            for (int i = 0; i < storages.Count; i++)
            {
                if (equalAmount > storages[i].MaxRemovablePower)
                {
                    power -= storages[i].RemovePower(storages[i].MaxRemovablePower);
                    equalAmount = power / (storages.Count - i - 1);
                }
                else
                {
                    power -= storages[i].RemovePower(equalAmount);
                }
            }
        }

        /// <summary>
        /// Distribute available power equally to power storages.
        /// TODO : should limit the amount of power a storage can receive in a single update to avoid instant charge.
        /// </summary>
        /// <param name="availablePower"> Power available after consuming.</param>
        /// <returns> Left over power if storages are full.</returns>
        private float ChargeStorages(float availablePower)
        {
            if (availablePower <= 0f)
            {
                return 0f;
            }

            // Order the list to make sure that storages to be fully charged come first
            List<IPowerStorage> notFullStorages = _storages.Where(x => x.RemainingCapacity > 0 && x.IsOn)
                .OrderBy(x => x.RemainingCapacity).ToList();

            if (notFullStorages.Count == 0)
            {
                return availablePower;
            }

            float equalAmount = availablePower / notFullStorages.Count;
            for (int i = 0; i < notFullStorages.Count; i++)
            {
                if (equalAmount > notFullStorages[i].RemainingCapacity)
                {
                    availablePower -= notFullStorages[i].AddPower(notFullStorages[i].RemainingCapacity);
                    equalAmount = availablePower / (notFullStorages.Count - i - 1);
                }
                else
                {
                    availablePower -= notFullStorages[i].AddPower(equalAmount);
                }
            }

            return availablePower;
        }

        private ApcControlFlags GetEnabledChannels()
        {
            if (_apcChannelSources.Count == 0)
            {
                return ApcControlFlags.All;
            }

            ApcControlFlags enabled = ApcControlFlags.None;
            foreach (IApcChannelSource apc in _apcChannelSources)
            {
                enabled |= apc.Channels;
            }

            return enabled;
        }

        private static List<IPowerConsumer> GetActiveConsumers(IReadOnlyList<IPowerConsumer> consumers, ApcControlFlags enabledChannels)
        {
            List<IPowerConsumer> activeConsumers = new();
            foreach (IPowerConsumer consumer in consumers)
            {
                if (IsChannelEnabled(consumer.Channel, enabledChannels))
                {
                    activeConsumers.Add(consumer);
                }
            }

            return activeConsumers;
        }

        private List<IPowerConsumer> GetActiveConsumers(ApcControlFlags enabledChannels)
        {
            return GetActiveConsumers(_consumers, enabledChannels);
        }

        private static bool IsChannelEnabled(PowerChannel channel, ApcControlFlags enabledChannels)
        {
            ApcControlFlags flag = channel switch
            {
                PowerChannel.Lighting => ApcControlFlags.Lighting,
                PowerChannel.Environment => ApcControlFlags.Environment,
                _ => ApcControlFlags.Equipment,
            };

            return (enabledChannels & flag) != 0;
        }

        private static float SumChannelLoad(IEnumerable<IPowerConsumer> consumers, PowerChannel channel)
        {
            return consumers.Where(consumer => consumer.Channel == channel).Sum(consumer => consumer.PowerNeeded);
        }
    }
}
