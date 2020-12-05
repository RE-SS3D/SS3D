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
            
            // TODO: Once we have a proper implementation of body parts/clothing this should not create containers on the creature root
            /*var inventory = transform.GetComponentInParent<InventoryUi>().Inventory;
            GameObject creature = inventory.Hands.GetComponentInParent<Creature>().gameObject;
            var slots = GetComponentsInChildren<SingleItemContainerSlot>();
            foreach (SingleItemContainerSlot slot in slots)
            {
                var attachedContainer = creature.AddComponent<AttachedContainer>();
                attachedContainer.Container = new Container{Size = new Vector2Int(10, 10)};
                slot.Container = attachedContainer;
                slot.Inventory = inventory;
            }
            
            creature.GetComponent<ContainerSync>().UpdateContainers();*/
        }
    }
}