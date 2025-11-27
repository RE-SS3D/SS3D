using SS3D.Systems.Inventory.Items;
using SS3D.Traits;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    public class FilterCondition : IStorageCondition
    {
        [Tooltip("The filter on the container.")]
        [SerializeField]
        private Filter _containerFilter;

        public bool CanStore(AttachedContainer container, Item item) => _containerFilter.CanStore(item);

        public bool CanRemove(AttachedContainer container, Item item) => true;
    }
}
