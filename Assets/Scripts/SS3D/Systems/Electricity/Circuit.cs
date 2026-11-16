using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Electricity
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
        private List<IApcChannelSource> _apcChannelSources;
        private Func<IPowerConsumer, ApcControlFlags> _getEnabledChannelsForConsumer;
        private Func<IPowerConsumer, bool> _includeInCableDistribution;

        private readonly List<IPowerConsumer> _activeConsumersScratch = new();
        private readonly List<IPowerConsumer> _poweredConsumersScratch = new();
        private readonly HashSet<IPowerConsumer> _poweredSetScratch = new();
        private readonly List<IPowerStorage> _storageScratch = new();

        private float _pendingProducerSurplus;

        public Circuit()
        {
            _consumers = new();
            _producers = new();
            _storages = new();
            _apcChannelSources = new();
        }

        public void SetConsumerChannelResolver(Func<IPowerConsumer, ApcControlFlags> getEnabledChannelsForConsumer)
        {
            _getEnabledChannelsForConsumer = getEnabledChannelsForConsumer;
        }

        public void SetCableDistributionFilter(Func<IPowerConsumer, bool> includeInCableDistribution)
        {
            _includeInCableDistribution = includeInCableDistribution;
        }

        internal ApcControlFlags GetCircuitWideEnabledChannels() => GetEnabledChannels();

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
            return BuildStats(apcCell, GetActiveConsumers());
        }

        public CircuitStats GetStatsForConsumers(IPowerStorage apcCell, IReadOnlyCollection<IPowerConsumer> scopedConsumers)
        {
            if (scopedConsumers == null || scopedConsumers.Count == 0)
            {
                return BuildStats(apcCell, new List<IPowerConsumer>());
            }

            HashSet<IPowerConsumer> scope = scopedConsumers as HashSet<IPowerConsumer> ?? scopedConsumers.ToHashSet();
            List<IPowerConsumer> activeConsumers = GetActiveConsumers().Where(consumer => scope.Contains(consumer)).ToList();
            return BuildStats(apcCell, activeConsumers);
        }

        public IReadOnlyList<IPowerConsumer> GetConsumers() => _consumers;

        public float GetProducerSupplyKw() => _producers.Sum(producer => producer.PowerProduction);

        private CircuitStats BuildStats(IPowerStorage apcCell, List<IPowerConsumer> activeConsumers)
        {
            float supplyKw = _producers.Sum(x => x.PowerProduction);
            float demandKw = activeConsumers.Sum(x => x.PowerNeeded);
            float batteryCharge = apcCell != null && apcCell.MaxCapacityKwh > 0f
                ? apcCell.StoredEnergyKwh / apcCell.MaxCapacityKwh
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
                BatteryDraining = supplyKw < demandKw && apcCell is { StoredEnergyKwh: > 0f, IsOn: true },
            };
        }

        /// <summary>
        /// Do an update on the whole circuit power. Produce power, consume power that needs to be consumed, and charge stuff that can be charged.
        /// </summary>
        public void UpdateCircuitPower(float tickSeconds = ElectricityUnits.DefaultTickSeconds)
        {
            UpdateCableDistributionOnly(tickSeconds);
            ChargePendingProducerSurplus(tickSeconds);
        }

        /// <summary>
        /// Powers cable-distributed consumers and records producer surplus for later SMES charging.
        /// Area-scoped consumers are handled separately via <see cref="DrawGridPowerForArea"/>.
        /// </summary>
        public void UpdateCableDistributionOnly(float tickSeconds = ElectricityUnits.DefaultTickSeconds)
        {
            FillActiveConsumers(_activeConsumersScratch);
            _pendingProducerSurplus = ConsumePower(
                _activeConsumersScratch,
                tickSeconds,
                _poweredConsumersScratch,
                _storageScratch);
            UpdateConsumerStatus(_poweredConsumersScratch);
        }

        /// <summary>
        /// Charge non-APC storages from producer surplus left after cable and area distribution.
        /// </summary>
        public void ChargePendingProducerSurplus(float tickSeconds = ElectricityUnits.DefaultTickSeconds)
        {
            if (_pendingProducerSurplus <= 0f)
            {
                _pendingProducerSurplus = 0f;
                return;
            }

            _pendingProducerSurplus = ChargeStorages(_pendingProducerSurplus, tickSeconds);
        }

        /// <summary>
        /// Grid headroom available to an APC on this circuit after cable loads are served this tick.
        /// </summary>
        public float GetAvailableGridSupplyForArea(float tickSeconds = ElectricityUnits.DefaultTickSeconds)
        {
            float storageSupply = 0f;
            for (int i = 0; i < _storages.Count; i++)
            {
                IPowerStorage storage = _storages[i];
                if (IsApcCellStorage(storage) || !storage.IsOn)
                {
                    continue;
                }

                storageSupply += storage.MaxDeliverableKw(tickSeconds);
            }

            return Math.Max(0f, _pendingProducerSurplus + storageSupply);
        }

        public float PendingProducerSurplus => _pendingProducerSurplus;

        /// <summary>
        /// Draw power from producers and non-APC storages on this circuit for area-scoped consumers.
        /// </summary>
        public float DrawGridPowerForArea(float requestedKw, float tickSeconds = ElectricityUnits.DefaultTickSeconds)
        {
            if (requestedKw <= 0f)
            {
                return 0f;
            }

            float remaining = requestedKw;
            float fromSurplus = Math.Min(remaining, _pendingProducerSurplus);
            remaining -= fromSurplus;
            _pendingProducerSurplus -= fromSurplus;

            if (remaining > 0f)
            {
                FillAvailableNonApcStorages(_storageScratch, tickSeconds);
                SortStoragesByDeliverableAscending(_storageScratch, tickSeconds);
                DrainBatteries(remaining, _storageScratch, tickSeconds);
            }

            return requestedKw - remaining;
        }

        /// <summary>
        /// Turn on or off consumers, depending on whether they are powered.
        /// </summary>
        /// <param name="poweredConsumers">Consumers, that were powered</param>
        private void UpdateConsumerStatus(List<IPowerConsumer> poweredConsumers)
        {
            _poweredSetScratch.Clear();
            for (int i = 0; i < poweredConsumers.Count; i++)
            {
                _poweredSetScratch.Add(poweredConsumers[i]);
            }

            foreach (IPowerConsumer consumer in _consumers)
            {
                if (_includeInCableDistribution != null && !_includeInCableDistribution(consumer))
                {
                    continue;
                }

                PowerStatus target = _poweredSetScratch.Contains(consumer)
                    ? PowerStatus.Powered
                    : PowerStatus.Inactive;
                if (consumer.PowerStatus != target)
                {
                    consumer.PowerStatus = target;
                    PowerStateVersion.Bump();
                }
            }
        }

        /// <summary>
        /// Try to satisfy consumers with producer output and non-APC storage discharge.
        /// </summary>
        /// <returns>Unused producer output in kW</returns>
        private float ConsumePower(
            List<IPowerConsumer> activeConsumers,
            float tickSeconds,
            List<IPowerConsumer> poweredConsumers,
            List<IPowerStorage> storageScratch)
        {
            float producerSupply = 0f;
            for (int i = 0; i < _producers.Count; i++)
            {
                producerSupply += _producers[i].PowerProduction;
            }

            FillAvailableNonApcStorages(storageScratch, tickSeconds);
            float batterySupply = 0f;
            for (int i = 0; i < storageScratch.Count; i++)
            {
                batterySupply += storageScratch[i].MaxDeliverableKw(tickSeconds);
            }

            float totalBudget = producerSupply + batterySupply;

            PowerConsumerAllocation.AllocateUnderBudget(activeConsumers, totalBudget, poweredConsumers);
            float poweredDemand = 0f;
            for (int i = 0; i < poweredConsumers.Count; i++)
            {
                poweredDemand += poweredConsumers[i].PowerNeeded;
            }

            float batteryDraw = Math.Max(0f, poweredDemand - producerSupply);

            if (batteryDraw > 0f)
            {
                SortStoragesByDeliverableAscending(storageScratch, tickSeconds);
                DrainBatteries(batteryDraw, storageScratch, tickSeconds);
            }

            return Math.Max(0f, producerSupply - poweredDemand);
        }

        /// <summary>
        /// Drain batteries from storages equally.
        /// </summary>
        /// <param name="powerKw">Power to drain in kW. Must be less than or equal to the sum of available discharge rates.</param>
        /// <param name="storages">Storages to drain from</param>
        private void DrainBatteries(float powerKw, List<IPowerStorage> storages, float tickSeconds)
        {
            if (storages.Count == 0 || powerKw <= 0f)
            {
                return;
            }

            float availableKw = 0f;
            for (int i = 0; i < storages.Count; i++)
            {
                availableKw += storages[i].MaxDeliverableKw(tickSeconds);
            }

            if (powerKw > availableKw)
            {
                Log.Error(this, "Energy requested for draining batteries is greater than available energy in batteries." +
                    "This will result in creating some free energy.");
            }

            float equalAmount = powerKw / storages.Count;
            for (int i = 0; i < storages.Count; i++)
            {
                float deliverable = storages[i].MaxDeliverableKw(tickSeconds);
                if (equalAmount > deliverable)
                {
                    powerKw -= storages[i].RemovePowerKw(deliverable, tickSeconds);
                    equalAmount = storages.Count - i - 1 > 0 ? powerKw / (storages.Count - i - 1) : 0f;
                }
                else
                {
                    powerKw -= storages[i].RemovePowerKw(equalAmount, tickSeconds);
                }
            }
        }

        /// <summary>
        /// Distribute available power equally to power storages.
        /// </summary>
        /// <param name="availablePowerKw">Producer surplus in kW after consuming.</param>
        /// <returns>Leftover power in kW if storages are full or charge-limited.</returns>
        private float ChargeStorages(float availablePowerKw, float tickSeconds)
        {
            if (availablePowerKw <= 0f)
            {
                return 0f;
            }

            _storageScratch.Clear();
            for (int i = 0; i < _storages.Count; i++)
            {
                IPowerStorage storage = _storages[i];
                if (storage.RemainingCapacityKwh > 0f
                    && storage.IsOn
                    && storage.MaxChargeRateKw > 0f
                    && !IsApcCellStorage(storage))
                {
                    _storageScratch.Add(storage);
                }
            }

            if (_storageScratch.Count == 0)
            {
                return availablePowerKw;
            }

            SortStoragesByRemainingCapacityAscending(_storageScratch);

            float equalAmount = availablePowerKw / _storageScratch.Count;
            for (int i = 0; i < _storageScratch.Count; i++)
            {
                float maxChargeKw = _storageScratch[i].MaxChargeRateKw;
                float chargeKw = Math.Min(equalAmount, maxChargeKw);
                float absorbed = _storageScratch[i].AddPowerKw(chargeKw, tickSeconds);
                availablePowerKw -= absorbed;
                equalAmount = _storageScratch.Count - i - 1 > 0 ? availablePowerKw / (_storageScratch.Count - i - 1) : 0f;
            }

            return availablePowerKw;
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

        private List<IPowerConsumer> GetActiveConsumers()
        {
            FillActiveConsumers(_activeConsumersScratch);
            return _activeConsumersScratch;
        }

        private void FillActiveConsumers(List<IPowerConsumer> results)
        {
            results.Clear();
            if (_getEnabledChannelsForConsumer == null)
            {
                ApcControlFlags enabledChannels = GetEnabledChannels();
                for (int i = 0; i < _consumers.Count; i++)
                {
                    IPowerConsumer consumer = _consumers[i];
                    if (PowerGate.IsChannelEnabled(consumer.Channel, enabledChannels))
                    {
                        results.Add(consumer);
                    }
                }

                return;
            }

            for (int i = 0; i < _consumers.Count; i++)
            {
                IPowerConsumer consumer = _consumers[i];
                if (_includeInCableDistribution != null && !_includeInCableDistribution(consumer))
                {
                    continue;
                }

                ApcControlFlags enabledChannels = _getEnabledChannelsForConsumer(consumer);
                if (PowerGate.IsChannelEnabled(consumer.Channel, enabledChannels))
                {
                    results.Add(consumer);
                }
            }
        }

        private void FillAvailableNonApcStorages(List<IPowerStorage> results, float tickSeconds)
        {
            results.Clear();
            for (int i = 0; i < _storages.Count; i++)
            {
                IPowerStorage storage = _storages[i];
                if (IsApcCellStorage(storage) || !storage.IsOn)
                {
                    continue;
                }

                if (storage.MaxDeliverableKw(tickSeconds) > 0f)
                {
                    results.Add(storage);
                }
            }
        }

        private static void SortStoragesByDeliverableAscending(List<IPowerStorage> storages, float tickSeconds)
        {
            // Insertion sort — storage lists are tiny; avoids Comparison<> alloc from List.Sort lambda.
            for (int i = 1; i < storages.Count; i++)
            {
                IPowerStorage current = storages[i];
                float currentDeliverable = current.MaxDeliverableKw(tickSeconds);
                int j = i - 1;
                while (j >= 0 && storages[j].MaxDeliverableKw(tickSeconds) > currentDeliverable)
                {
                    storages[j + 1] = storages[j];
                    j--;
                }

                storages[j + 1] = current;
            }
        }

        private static void SortStoragesByRemainingCapacityAscending(List<IPowerStorage> storages)
        {
            for (int i = 1; i < storages.Count; i++)
            {
                IPowerStorage current = storages[i];
                float currentCapacity = current.RemainingCapacityKwh;
                int j = i - 1;
                while (j >= 0 && storages[j].RemainingCapacityKwh > currentCapacity)
                {
                    storages[j + 1] = storages[j];
                    j--;
                }

                storages[j + 1] = current;
            }
        }

        private static float SumChannelLoad(IEnumerable<IPowerConsumer> consumers, PowerChannel channel)
        {
            return consumers.Where(consumer => consumer.Channel == channel).Sum(consumer => consumer.PowerNeeded);
        }

        private static bool IsApcCellStorage(IPowerStorage storage) => storage is IApcChannelSource;
    }
}
