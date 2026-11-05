using SS3D.Core.Behaviours;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// A suit can't be removed while its storage holds a tank, like a jumpsuit with a belt on it.
    /// </summary>
    public class ExoSuitStorageCondition : Actor, IStorageCondition
    {
        [SerializeField]
        private AttachedContainer _suitStorageContainer;

        public bool CanRemove(AttachedContainer container, Item item)
        {
            return _suitStorageContainer.Empty;
        }

        public bool CanStore(AttachedContainer container, Item item)
        {
            return true;
        }
    }
}
