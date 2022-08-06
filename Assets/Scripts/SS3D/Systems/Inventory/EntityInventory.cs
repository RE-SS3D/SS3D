using System.Collections.Generic;
using System.Linq;
using SS3D.Core.Systems.Inventory.Subsystem.Clothing;
using UnityEngine;

namespace SS3D.Core.Systems.Inventory
{
    /// <summary>
    /// Base class for all entities's inventory
    /// </summary>
    public class EntityInventory : MonoBehaviour
    {
        [SerializeField] private List<InventorySlot> _slots;

        /// <summary>
        /// Gets clothing slots from inventory, use T as the specific type of slot
        /// TODO: Proper cache of this information, considering slots can change at runtime, like adding an arm/other type of hand 
        /// </summary>
        /// <returns></returns>
        public List<T> GetSlots<T>()
        {
            List<T> slots = new();

            foreach (InventorySlot inventorySlot in _slots)
            {
                if (inventorySlot is T type)
                {
                    slots.Add(type);
                }
            }

            return slots;
        }
    }
}