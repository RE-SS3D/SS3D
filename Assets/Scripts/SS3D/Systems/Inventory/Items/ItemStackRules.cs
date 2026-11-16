using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    /// <summary>
    /// Pure stack arithmetic shared by <see cref="Item"/> and container merges, so EditMode tests can
    /// check it without a spawned item (inventory-storage.md §5).
    /// </summary>
    public static class ItemStackRules
    {
        /// <summary>A count clamped to what one stack of this definition can hold (at least 1).</summary>
        public static int ClampCount(int count, int maxStackSize) => Mathf.Clamp(count, 1, Mathf.Max(1, maxStackSize));

        /// <summary>Whether <paramref name="incoming"/> fits whole into an existing stack. No partial merges.</summary>
        public static bool CanMerge(int existing, int incoming, int maxStackSize) =>
            maxStackSize > 1 && existing + incoming <= maxStackSize;

        /// <summary>
        /// What consuming <paramref name="amount"/> from a stack of <paramref name="count"/> leaves. False
        /// (nothing spent) when the stack holds fewer or <paramref name="amount"/> is not positive.
        /// <paramref name="deleteItem"/> is true when the stack is used up.
        /// </summary>
        public static bool TryResolveConsume(int count, int amount, out int remaining, out bool deleteItem)
        {
            remaining = count;
            deleteItem = false;

            if (amount <= 0 || count < amount)
            {
                return false;
            }

            remaining = count - amount;
            deleteItem = remaining <= 0;
            return true;
        }
    }
}
