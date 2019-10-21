using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * The hands are containers for objects that also affect the player's interaction system.
 */
[RequireComponent(typeof(Interaction))]
public class Hands : Container, Tool
{
    // Set the slot requirements
    public Hands()
    {
        // TODO: Clean this up
        slots = new SlotRequirements[] {
            new SlotRequirements(),
            new SlotRequirements()
        };
        slots[0].itemRestrictions = new string[0];
        slots[0].count = 1;
        slots[1].itemRestrictions = new string[0];
        slots[1].count = 1;
    }

    /**
     * The default hand interaction when no object is present.
     * Note: This could be moved into a different class if this one gets too cluttered
     */
    public void Interact(GameObject interactedObject, bool secondary = false)
    {
        ItemReference itemRef = interactedObject.GetComponent<ItemReference>();
        if(itemRef)
            PutItemInHand(itemRef);

        // TODO: Default hand interactions with non-items
    }

    // Override add item so any changes refresh the interaction system's tool
    public override void AddItem(int index, Item item)
    {
        base.AddItem(index, item);
        interactionSystem.selectedTool = items[selectedHand] is Tool ? items[selectedHand] as Tool : this;
    }

    // Same override for RemoveItem
    public override void RemoveItem(int index)
    {
        base.RemoveItem(index);
        interactionSystem.selectedTool = items[selectedHand] is Tool ? items[selectedHand] as Tool : this;
    }

    [System.NonSerialized]
    public int selectedHand = 0;


    private void Start()
    {
        interactionSystem = GetComponent<Interaction>();
        interactionSystem.selectedTool = this;
    }

    private void Update()
    {
        // TODO: Ensure both hands are usable
        if (Input.GetButtonDown("SwapActive"))
        {
            selectedHand = 1 - selectedHand;
            interactionSystem.selectedTool = items[selectedHand] is Tool ? items[selectedHand] as Tool : this;
        }
    }

    /**
     * Takes the given spawned item and moves it into the player's hand
     */
    [Command]
    private void PutItemInHand(ItemReference itemRef)
    {
        AddItem(selectedHand, itemRef.item);
        Destroy(itemRef.gameObject);
    }

    private Interaction interactionSystem;
}
