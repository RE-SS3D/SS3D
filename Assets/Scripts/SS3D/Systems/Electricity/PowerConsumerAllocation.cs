using System.Collections.Generic;

namespace System.Electricity
{
    /// <summary>
    /// Allocates power to consumers by channel priority.
    /// Inclusion order matches restore priority: Lighting, then Environment, then Equipment.
    /// </summary>
    public static class PowerConsumerAllocation
    {
        private static readonly PowerChannel[] InclusionOrder =
        {
            PowerChannel.Lighting,
            PowerChannel.Environment,
            PowerChannel.Equipment,
        };

        public static List<IPowerConsumer> AllocateUnderBudget(
            IReadOnlyList<IPowerConsumer> consumers,
            float budgetKw)
        {
            var poweredConsumers = new List<IPowerConsumer>();
            if (budgetKw <= 0f || consumers == null || consumers.Count == 0)
            {
                return poweredConsumers;
            }

            float remainingBudget = budgetKw;
            foreach (PowerChannel channel in InclusionOrder)
            {
                foreach (IPowerConsumer consumer in consumers)
                {
                    if (consumer.Channel != channel || consumer.PowerNeeded > remainingBudget)
                    {
                        continue;
                    }

                    poweredConsumers.Add(consumer);
                    remainingBudget -= consumer.PowerNeeded;
                }
            }

            return poweredConsumers;
        }
    }
}
