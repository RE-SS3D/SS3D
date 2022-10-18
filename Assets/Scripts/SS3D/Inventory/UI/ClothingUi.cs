using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.UI;
using SS3D.Systems.Entities;
using UnityEngine;

namespace SS3D.Inventory.UI
{
    public class ClothingUi : MonoBehaviour
    {
        public void Start()
        {
            // Connects ui clothing slots to containers on the creature
            Inventory inventory = transform.GetComponentInParent<InventoryUi>().Inventory;
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