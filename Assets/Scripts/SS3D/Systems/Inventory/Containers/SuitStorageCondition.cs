using SS3D.Core.Behaviours;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Suit storage exists only while a suit is worn and holds a gas tank (inventory-storage.md §3).
    /// </summary>
    public class SuitStorageCondition : Actor, IStorageCondition
    {
        [SerializeField]
        private AttachedContainer _suitContainer;

        public bool CanRemove(AttachedContainer container, Item item)
        {
            return true;
        }

        public bool CanStore(AttachedContainer container, Item item)
        {
            return !_suitContainer.Empty && item.TryGetComponent(out GasTankItemExtension _);
        }
    }
}
