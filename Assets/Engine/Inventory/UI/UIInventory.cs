using SS3D.Engine.Interactions.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Engine.Inventory.UI
{
    /**
     * Connects a player's inventory with the UI.
     * 
     * The UIInventory collects containers the player has access to them, and
     * hands them out to UI Elements.
     * 
     * Containers attached to the player are dealt with two 'special' ui elements, one for the body, one for the hotbar.
     * All other containers are handled by a creating a 'generic' container.
    */
    public class UIInventory : MonoBehaviour, UIAbstractContainer.UIInventoryHandler
    {
        // The prefab for when a new container needs to be made
        public GameObject genericContainerPrefab;
        // The existing ui element for the player body
        public UIAbstractContainer playerContainer;

        public void StartUI(Inventory inventory)
        {
            this.inventory = inventory;

            playerContainer.owner = inventory.gameObject;
            playerContainer.inventoryHandler = this;

            handlers.Add(playerContainer);

            inventory.EventOnChange += (a, b, c, d) => OnInventoryChange();
            OnInventoryChange();
        }
    
        // Implement UIInventoryHandler
        public UIAbstractContainer FindHandler(Container container)
        {
            return handlers.Find(uiContainer => uiContainer.containers.Contains(container));
        }
        public UIAbstractContainer.SlotInfo GetHoldingSlot()
        {
            UIItemSlot uiSlot = FindHandler(inventory.holdingSlot.container).GetUISlot(inventory.holdingSlot.container, inventory.holdingSlot.slotIndex);
            return new UIAbstractContainer.SlotInfo(inventory.holdingSlot.container, inventory.holdingSlot.slotIndex, uiSlot);
        }
        public bool CanMoveItem(Container from, int fromSlot, Container to, int toSlot)
        {
            return Container.AreCompatible(to.GetSlot(toSlot), from.GetItem(fromSlot).itemType) && to.GetItem(toSlot) == null;
        }
        public bool CanMoveItem(Container from, int fromSlot, UIAbstractContainer to)
        {
            return false; // TODO
        }
        public void MoveItem(Container from, int fromSlot, Container to, int toSlot)
        {
            if (CanMoveItem(from, fromSlot, to, toSlot))
                inventory.CmdMoveItem(from.gameObject, fromSlot, to.gameObject, toSlot);
        }
        public void MoveItem(Container from, int fromSlot, UIAbstractContainer to)
        {
            // TODO:
            throw new NotImplementedException();
        }
        public void SetHeldItemSupply(float amount)
        {
            UIItemSlot uiSlot = FindHandler(inventory.holdingSlot.container).GetUISlot(inventory.holdingSlot.container, inventory.holdingSlot.slotIndex);
            uiSlot.SetItemSupplyDisplay(amount);
        }
        public void MoveSelectedItem(Container container, int slot)
        {
            if(inventory.holdingSlot.container == null)
                return;

            // If there is an item selected, move the selected item into the given slot.
            // Else if no item is currently held, move the selected item into the the holding slot.
            try {
                if (inventory.holdingSlot.container.GetItem(inventory.holdingSlot.slotIndex) != null && container.GetItem(slot) == null)
                    inventory.CmdMoveItem(inventory.holdingSlot.container.gameObject, inventory.holdingSlot.slotIndex, container.gameObject, slot);
                else if (inventory.holdingSlot.container.GetItem(inventory.holdingSlot.slotIndex) == null && container.GetItem(slot) != null)
                    inventory.CmdMoveItem(container.gameObject, slot, inventory.holdingSlot.container.gameObject, inventory.holdingSlot.slotIndex);
            }
            catch(Inventory.InventoryOperationException) { }
        }
        public void DropItem(Container from, int fromSlot, Vector2 screenPosition)
        {
            // Create a raycast to determine where to place the item
            RaycastHit hit;
            var found = Physics.Raycast(Camera.main.ScreenPointToRay(screenPosition), out hit);
            if(!found)
                return;
        
            inventory.CmdPlaceItem(from.gameObject, fromSlot, hit.point + hit.normal * 0.2f);
        }

        private void OnInventoryChange()
        {
            var containers = inventory.GetContainers();
            // Collect each container by owner
            Dictionary<GameObject, List<Container>> containerSets = containers.GroupBy(container => container.gameObject).ToDictionary(group => group.Key, group => group.ToList());

            foreach (var containerSet in containerSets)
            {
                // If a handler for it exists
                var handler = handlers.Find(renderer => renderer.owner == containerSet.Key);
                if (handler != null)
                {
                    if (!handler.containers.SequenceEqual(containerSet.Value))
                        handler.UpdateContainers(containerSet.Key, containerSet.Value);
                }
                else
                {
                    // No handler exists so we create a new one.
                    GameObject obj = Instantiate(genericContainerPrefab, new Vector2(100f, 100f), new Quaternion(), transform);
                    handler = obj.GetComponent<UIAbstractContainer>();
                    handler.inventoryHandler = this;
                    handler.UpdateContainers(containerSet.Key, containerSet.Value);
                    handlers.Add(handler);
                }
            }

            // Remove any no-longer-needed handlers (excluding player container)
            var removeList = handlers.FindAll(handler => !containerSets.Keys.Contains(handler.owner) && handler.owner != inventory.gameObject);
            foreach (var handler in removeList)
            {
                handler.Unlink();
                Destroy(handler.gameObject);
            }

            handlers = handlers.Except(removeList).ToList();
        }

        public void OnDragStart(UIAbstractContainer.SlotInfo slot, PointerEventData eventData)
        {
            draggingOperation = gameObject.AddComponent<DraggingOperation>();
            draggingOperation.uiInventory = this;
            draggingOperation.canvas = gameObject;

            // If the drag isn't valid, fail to drag.
            if (!draggingOperation.OnBeginDrag(slot, eventData))
            {
                eventData.pointerDrag = null;
                Destroy(draggingOperation);
            }
        }

        private Inventory inventory;
        private List<UIAbstractContainer> handlers = new List<UIAbstractContainer>();
    
        private DraggingOperation draggingOperation;
    }
}