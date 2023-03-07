using Coimbra;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.UI
{
    public class ClothingUi : MonoBehaviour
    {
        public void Start()
        {
            // Connects ui clothing slots to containers on the creature
            Containers.Inventory inventory = transform.GetComponentInParent<InventoryView>().Inventory;
            GameObject creature = inventory.Hands.GetComponentInParent<Entity>().gameObject;
            ClothingContainers clothingContainers = creature.GetComponent<ClothingContainers>();
            SingleItemContainerSlot[] slots = GetComponentsInChildren<SingleItemContainerSlot>();
            foreach (SingleItemContainerSlot slot in slots)
            {
                if (!clothingContainers.Containers.TryGetValue(slot.name, out AttachedContainer container))
                {
                    continue;
                }

                slot.Inventory = inventory;
                slot.Container = container;
            }
        }
    }
}