using SS3D.Storage.Containers;
using SS3D.Systems.Entities;
using SS3D.Systems.Storage.Containers;
using UnityEngine;

namespace SS3D.Storage.UI
{
    public class ClothingUi : MonoBehaviour
    {
        public void Start()
        {
            // Connects ui clothing slots to containers on the creature
            Storage.Inventory inventory = transform.GetComponentInParent<InventoryUi>().Inventory;
            GameObject creature = inventory.Hands.GetComponentInParent<PlayerControllable>().gameObject;
            ClothingContainers clothingContainers = creature.GetComponent<ClothingContainers>();
            SingleItemContainerSlot[] slots = GetComponentsInChildren<SingleItemContainerSlot>();
            foreach (SingleItemContainerSlot slot in slots)
            {
                if (clothingContainers.Containers.TryGetValue(slot.name, out AttachedContainer container))
                {
                    slot.Inventory = inventory;
                    slot.Container = container;
                }
            }
        }
    }
}