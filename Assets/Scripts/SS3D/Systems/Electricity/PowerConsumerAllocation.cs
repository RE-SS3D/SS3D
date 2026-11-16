using System.Collections.Generic;

namespace SS3D.Systems.Electricity
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
            AllocateUnderBudget(consumers, budgetKw, poweredConsumers);
            return poweredConsumers;
        }

        /// <summary>Hot-path variant: clears and fills <paramref name="results"/> with no new List.</summary>
        public static void AllocateUnderBudget(
            IReadOnlyList<IPowerConsumer> consumers,
            float budgetKw,
            List<IPowerConsumer> results)
        {
            results.Clear();
            if (budgetKw <= 0f || consumers == null || consumers.Count == 0)
            {
                return;
            }

            float remainingBudget = budgetKw;
            foreach (PowerChannel channel in InclusionOrder)
            {
                for (int i = 0; i < consumers.Count; i++)
                {
                    IPowerConsumer consumer = consumers[i];
                    if (consumer.Channel != channel || consumer.PowerNeeded > remainingBudget)
                    {
                        continue;
                    }

                    results.Add(consumer);
                    remainingBudget -= consumer.PowerNeeded;
                }
            }
        }
    }
}
