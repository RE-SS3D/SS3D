using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Storage that lives on an item (backpack grid, toolbelt, box, PDA card slot), as opposed to the equip slot or
    /// hand the item sits in. One rule for every inventory surface: the Main HUD wells and storage panel slots both
    /// decide here whether a slot shows its open button and whether a click with a held item stores it inside.
    /// </summary>
    public static class ItemStorage
    {
        /// <summary>
        /// The storage container owned by <paramref name="item"/>. Prefers <see cref="AttachedContainer.HasUi"/> content
        /// grids; never <see cref="Item.Container"/>, which is where the item is stored, not storage on it. Containers of
        /// items nested inside are skipped, and so are body equip mounts (<see cref="AttachedContainer.DisplayAsSlotInUI"/>).
        /// </summary>
        public static bool TryGetStorageOn(Item item, out AttachedContainer storage)
        {
            storage = null;
            if (item == null)
            {
                return false;
            }

            AttachedContainer fallback = null;
            foreach (AttachedContainer candidate in item.GetComponentsInChildren<AttachedContainer>())
            {
                if (candidate.GetComponentInParent<Item>() != item)
                {
                    continue;
                }

                if (candidate.HasUi)
                {
                    storage = candidate;
                    return true;
                }

                if (fallback == null && !candidate.DisplayAsSlotInUI)
                {
                    fallback = candidate;
                }
            }

            storage = fallback;
            return storage != null;
        }

        /// <summary>
        /// Where <paramref name="held"/> would go if the player clicked <paramref name="storageItem"/> with it: false when
        /// the storage item has no storage, is the held item itself, is not reachable or open to this inventory, or the
        /// held item does not fit.
        /// </summary>
        public static bool TryFindStoreTarget(
            HumanInventory inventory,
            Item held,
            Item storageItem,
            out AttachedContainer storage,
            out Vector2Int position)
        {
            position = default;
            storage = null;
            if (inventory == null
                || held == null
                || held == storageItem
                || !TryGetStorageOn(storageItem, out storage)
                || storage.GetComponentInParent<Item>() == held
                || !storage.IsAccessibleBy(inventory)
                || !storage.TryFindPositionFor(held, out position))
            {
                return false;
            }

            IStorageAccessGate gate = storage.GetComponentInParent<IStorageAccessGate>();
            return gate == null || gate.AllowsStorageAccess;
        }
    }
}
