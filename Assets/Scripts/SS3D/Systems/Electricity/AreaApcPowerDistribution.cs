using SS3D.Core;
using SS3D.Systems.Area;
using System;
using System.Collections.Generic;
using System.Linq;

namespace System.Electricity
{
    /// <summary>
    /// Powers consumers from their area APC without requiring a cable path to each device.
    /// Grid supply comes from the APC's cable circuit; the APC cell covers local deficits.
    /// </summary>
    public static class AreaApcPowerDistribution
    {
        private static Random RandomGenerator = new();
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

        public static CircuitStats BuildApcStats(
            float gridSupplyKw,
            IPowerStorage apcCell,
            IReadOnlyList<IPowerConsumer> activeAreaConsumers)
        {
            float demandKw = activeAreaConsumers.Sum(consumer => consumer.PowerNeeded);
            float batteryCharge = apcCell != null && apcCell.MaxCapacity > 0f
                ? apcCell.StoredPower / apcCell.MaxCapacity
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
                BatteryDraining = gridSupplyKw < demandKw && apcCell is { StoredPower: > 0f, IsOn: true },
            };
        }

        public static void PowerAreaConsumers(
            IApcChannelSource apc,
            IPowerStorage apcCell,
            float gridSupplyKw,
            IReadOnlyList<IPowerConsumer> areaConsumers,
            IReadOnlyList<IPowerConsumer> activeAreaConsumers)
        {
            float demandKw = activeAreaConsumers.Sum(consumer => consumer.PowerNeeded);
            float deficitKw = demandKw - gridSupplyKw;
            List<IPowerConsumer> poweredConsumers = new(activeAreaConsumers);

            if (deficitKw > 0f)
            {
                float availableCellKw = apcCell?.MaxRemovablePower ?? 0f;
                if (deficitKw > availableCellKw)
                {
                    poweredConsumers = ShedLoad(activeAreaConsumers.ToList(), deficitKw - availableCellKw);
                    deficitKw = poweredConsumers.Sum(consumer => consumer.PowerNeeded) - gridSupplyKw;
                }

                if (deficitKw > 0f && apcCell != null)
                {
                    apcCell.RemovePower(Math.Min(deficitKw, apcCell.MaxRemovablePower));
                }
            }
            else if (deficitKw < 0f && apcCell != null)
            {
                apcCell.AddPower(-deficitKw);
            }

            HashSet<IPowerConsumer> poweredSet = poweredConsumers.ToHashSet();
            foreach (IPowerConsumer consumer in areaConsumers)
            {
                consumer.PowerStatus = poweredSet.Contains(consumer) ? PowerStatus.Powered : PowerStatus.Inactive;
            }
        }

        private static List<IPowerConsumer> ShedLoad(List<IPowerConsumer> activeConsumers, float deficitKw)
        {
            var poweredConsumers = new List<IPowerConsumer>(activeConsumers);
            float neededReduction = deficitKw;

            while (neededReduction > 0f && poweredConsumers.Count > 0)
            {
                List<IPowerConsumer> candidates = poweredConsumers
                    .Where(consumer => consumer.PowerNeeded >= neededReduction)
                    .ToList();
                if (candidates.Count == 0)
                {
                    candidates.AddRange(poweredConsumers);
                }

                IPowerConsumer removed = candidates[RandomGenerator.Next(candidates.Count)];
                neededReduction -= removed.PowerNeeded;
                poweredConsumers.Remove(removed);
            }

            return poweredConsumers;
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
