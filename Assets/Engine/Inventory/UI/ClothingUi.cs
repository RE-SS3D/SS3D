using Mirror;
using UnityEngine;

namespace SS3D.Engine.Inventory.UI
{
    public class ClothingUi : MonoBehaviour
    {
        public void Start()
        {
            if (NetworkServer.active && !NetworkClient.active)
            {
                Destroy(this);
                return;
            }
            
            // Connects ui clothing slots to containers on the creature
            var inventory = transform.GetComponentInParent<InventoryUi>().Inventory;
            GameObject creature = inventory.Hands.GetComponentInParent<Creature>().gameObject;
            var clothingContainers = creature.GetComponent<ClothingContainers>();
            var slots = GetComponentsInChildren<SingleItemContainerSlot>();
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