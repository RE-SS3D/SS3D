using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Simple class to mark that a container should be treated as the kind that can contain clothes only.
    /// </summary>
    public class ClothContainer : MonoBehaviour, IStorageCondition
    {
        [SerializeField]
        private ClothType _clothType;

        public ClothType ClothType => _clothType;

        /// <summary>A pair lives on its first side, so the second side stays empty while the first is on the body.</summary>
        public bool CanStore(AttachedContainer container, Item item)
        {
            ContainerType firstSide = PairedClothing.FirstSideOf(container.Type);
            HumanInventory inventory = GetComponentInParent<HumanInventory>();
            return firstSide == ContainerType.None || inventory == null || !inventory.TryGetTypeContainer(firstSide, 0, out _);
        }

        public bool CanRemove(AttachedContainer container, Item item)
        {
            return true;
        }
    }
}