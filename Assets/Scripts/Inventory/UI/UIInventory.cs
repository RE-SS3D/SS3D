using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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
    public UIAbstractContainer playerBodyView;
    // The existing ui element for the hotbar area
    public UIAbstractContainer hotbarView;

    // Implement UIInventoryHandler
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
        inventory.CmdMoveItem(from.gameObject, fromSlot, to.gameObject, toSlot);
    }
    public void MoveItem(Container from, int fromSlot, UIAbstractContainer to)
    {
        // TODO:
        throw new NotImplementedException();
    }
    public void MoveSelectedItem(Container container, int slot)
    {
        // If there is an item selected, move the selected item into the given slot.
        if(inventory.selectedSlot.container != null && inventory.selectedSlot.container.GetItem(inventory.selectedSlot.slotIndex) != null)
            inventory.CmdMoveItem(inventory.selectedSlot.container.gameObject, inventory.selectedSlot.slotIndex, container.gameObject, slot);
    }
    public void DropItem(Container from, int fromSlot, Vector2 screenPosition)
    {
        // Create a raycast to determine where to place the item
        RaycastHit hit;
        var found = Physics.Raycast(Camera.main.ScreenPointToRay(screenPosition), out hit);
        if(!found)
            return;
        
        inventory.CmdPlaceItem(from.gameObject, fromSlot, hit.point + hit.normal * 0.2f, new Quaternion());
    }

    public void StartUI(Inventory inventory)
    {
        this.inventory = inventory;
        playerBodyView.inventoryHandler = this;
        hotbarView.inventoryHandler = this;
        inventory.EventOnChange += (a, b, c) => OnInventoryChange();
        OnInventoryChange();
    }

    private void OnInventoryChange()
    {
        var containers = inventory.GetContainers();
        // Collect each container by owner
        Dictionary<GameObject, List<Container>> containerSets = containers.GroupBy(container => container.gameObject).ToDictionary(group => group.Key, group => group.ToList());
        
        foreach (var containerSet in containerSets)
        {
            // If this is the player, set the specific player views
            if(containerSet.Value.Count > 0 && containerSet.Value[0].isLocalPlayer)
            {
                playerBodyView.UpdateContainers(containerSet.Key, containerSet.Value);
                hotbarView.UpdateContainers(containerSet.Key, containerSet.Value);
                continue;
            }

            // If a handler for it exists
            var handler = handlers.Find(renderer => renderer.owner == containerSet.Key);
            if (handler != null)
            {
                if(!handler.containers.SequenceEqual(containerSet.Value))
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

        // Remove any no-longer-needed handlers
        var removeList = handlers.FindAll(handler => !containerSets.Keys.Contains(handler.owner));
        foreach (var handler in removeList)
            Destroy(handler);
        handlers = handlers.Except(removeList).ToList();
    }


    private List<UIAbstractContainer> handlers = new List<UIAbstractContainer>();
    private Inventory inventory;
}