using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Inventory
{
    /// <summary>
    /// Base class for all entities's inventory
    /// </summary>
    public class EntityInventory : MonoBehaviour
    {
        [SerializeField] private List<InventorySlot> _slots;

        /// <summary>
        /// Gets clothing slots from inventory, use T as the specific type of slot
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> GetSlots<T>() => _slots.OfType<T>();
    }
}