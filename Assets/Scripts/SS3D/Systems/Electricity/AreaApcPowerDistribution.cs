using SS3D.Core;
using SS3D.Systems.Area;
using System.Collections.Generic;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Powers consumers from their area APC without requiring a cable path to each device.
    /// Grid supply comes from the APC's cable circuit; the APC cell covers local deficits.
    /// </summary>
    public static class AreaApcPowerDistribution
    {
        public static bool IsAreaScopedConsumer(IPowerConsumer consumer)
        {
            return consumer is IElectricDevice device
                && SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                && areaSubSystem.TryGetEffectiveApcForDevice(device, out _);
        }

        public static List<IPowerConsumer> GetConsumersForApc(
            IApcChannelSource apc,
            IReadOnlyList<IPowerConsumer> registeredConsumers)
        {
            var scoped = new List<IPowerConsumer>();
            if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return scoped;
            }

            foreach (IPowerConsumer consumer in registeredConsumers)
            {
                if (consumer is IElectricDevice device
                    && areaSubSystem.TryGetEffectiveApcForDevice(device, out IApcChannelSource effectiveApc)
                    && ReferenceEquals(effectiveApc, apc))
                {
                    scoped.Add(consumer);
                }
            }

            return scoped;
        }

        public static List<IPowerConsumer> GetActiveConsumers(
            IEnumerable<IPowerConsumer> consumers,
            ApcControlFlags enabledChannels)
        {
            var activeConsumers = new List<IPowerConsumer>();
            foreach (IPowerConsumer consumer in consumers)
            {
                if (IsChannelEnabled(consumer.Channel, enabledChannels))
                {
                    activeConsumers.Add(consumer);
                }
            }

            return activeConsumers;
        }

        public static bool IsChannelEnabled(PowerChannel channel, ApcControlFlags enabledChannels) =>
            PowerGate.IsChannelEnabled(channel, enabledChannels);

        public static CircuitStats BuildApcStats(
            float gridSupplyKw,
            IPowerStorage apcCell,
            IReadOnlyList<IPowerConsumer> activeAreaConsumers)
        {
            float demandKw = SumPowerNeeded(activeAreaConsumers);
            float batteryCharge = apcCell != null && apcCell.MaxCapacityKwh > 0f
                ? apcCell.StoredEnergyKwh / apcCell.MaxCapacityKwh
                : 0f;

            return new CircuitStats
            {
                TotalSupplyKw = gridSupplyKw,
                TotalDemandKw = demandKw,
                ApcBatteryCharge = batteryCharge,
                LightingLoadKw = SumChannelLoad(activeAreaConsumers, PowerChannel.Lighting),
                EquipmentLoadKw = SumChannelLoad(activeAreaConsumers, PowerChannel.Equipment),
                EnvironmentLoadKw = SumChannelLoad(activeAreaConsumers, PowerChannel.Environment),
                GridMeetsLoad = gridSupplyKw >= demandKw,
                BatteryDraining = gridSupplyKw < demandKw && apcCell is { StoredEnergyKwh: > 0f, IsOn: true },
            };
        }

        public static void PowerAreaConsumers(
            IApcChannelSource apc,
            IPowerStorage apcCell,
            float gridSupplyKw,
            IReadOnlyList<IPowerConsumer> areaConsumers,
            IReadOnlyList<IPowerConsumer> activeAreaConsumers,
            float tickSeconds = ElectricityUnits.DefaultTickSeconds)
        {
            float cellDeliverableKw = apcCell is { IsOn: true } ? apcCell.MaxDeliverableKw(tickSeconds) : 0f;
            float totalBudgetKw = gridSupplyKw + cellDeliverableKw;
            List<IPowerConsumer> poweredConsumers = PowerConsumerAllocation.AllocateUnderBudget(activeAreaConsumers, totalBudgetKw);

            float poweredDemandKw = SumPowerNeeded(poweredConsumers);
            float cellDrawKw = poweredDemandKw - gridSupplyKw;

            if (cellDrawKw > 0f && apcCell != null)
            {
                apcCell.RemovePowerKw(cellDrawKw, tickSeconds);
            }
            else if (cellDrawKw < 0f && apcCell != null)
            {
                apcCell.AddPowerKw(-cellDrawKw, tickSeconds);
            }

            // Assign final status once per consumer. Setting Inactive then Powered every tick
            // flickers SyncVar OnChange (e.g. airlock close timers never fire — fixed thrice).
            HashSet<IPowerConsumer> poweredSet = new HashSet<IPowerConsumer>(poweredConsumers);
            for (int i = 0; i < areaConsumers.Count; i++)
            {
                IPowerConsumer consumer = areaConsumers[i];
                PowerStatus target = poweredSet.Contains(consumer)
                    ? PowerStatus.Powered
                    : PowerStatus.Inactive;
                if (consumer.PowerStatus != target)
                {
                    consumer.PowerStatus = target;
                }
            }
        }

        public static float SumPowerNeeded(IReadOnlyList<IPowerConsumer> consumers)
        {
            float sum = 0f;
            for (int i = 0; i < consumers.Count; i++)
            {
                sum += consumers[i].PowerNeeded;
            }

            return sum;
        }

        private static float SumChannelLoad(IReadOnlyList<IPowerConsumer> consumers, PowerChannel channel)
        {
            float sum = 0f;
            for (int i = 0; i < consumers.Count; i++)
            {
                IPowerConsumer consumer = consumers[i];
                if (consumer.Channel == channel)
                {
                    sum += consumer.PowerNeeded;
                }
            }

            return sum;
        }
    }
}
