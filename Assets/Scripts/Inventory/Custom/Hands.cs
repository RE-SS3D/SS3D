using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * The hands are containers for objects that also affect the player's interaction system.
 */
[RequireComponent(typeof(Interaction))]
[RequireComponent(typeof(Inventory))]
public class Hands : Container, Tool
{
    /**
     * The default hand interaction when no object is present.
     * Note: This could be moved into a different class if this one gets too cluttered
     */
    public void Interact(GameObject interactedObject, bool secondary = false)
    {
        Item item = interactedObject.GetComponent<Item>();
        if(item)
            inventory.CmdAddItem(item, this, selectedHand);

        // TODO: Default hand interactions with non-items
    }

    // Override add item so any changes refresh the interaction system's tool
    public override void AddItem(int index, Item item)
    {
        base.AddItem(index, item);
        UpdateInteraction();
    }
    // Same override for RemoveItem
    public override Item RemoveItem(int index)
    {
        Item item = base.RemoveItem(index);
        UpdateInteraction();
        return item;
    }

    [System.NonSerialized]
    public int selectedHand = 0;

    // Set the slot requirements defaults
    private void Reset()
    {
        // Set defaults for container.
        containerName = "Hands";
        containerType = Type.Interactors;
        slots = new SlotType[2];
        slots[0] = SlotType.LeftHand;
        slots[1] = SlotType.RightHand;

        UpdateInteraction();
    }
    private void Start()
    {
        interactionSystem = GetComponent<Interaction>();
        inventory = GetComponent<Inventory>();
        UpdateInteraction();
    }
    private void Update()
    {
        // TODO: Ensure both hands are usable
        if (Input.GetButtonDown("SwapActive"))
        {
            selectedHand = 1 - selectedHand;
            UpdateInteraction();
        }
    }

    private void UpdateInteraction()
    {
        interactionSystem.selectedTool = GetItem(selectedHand) is Tool ? GetItem(selectedHand) as Tool : this;
    }

    private Interaction interactionSystem;
    private Inventory inventory;
}
