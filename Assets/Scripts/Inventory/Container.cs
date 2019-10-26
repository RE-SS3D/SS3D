using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A container holds items of a given kind
 */
public class Container : NetworkBehaviour
{
    #region Classes and Types
    // TODO: We should investigate whether we should use something other than enums, e.g. just strings.

    // This is the types of container that are currently defined and needed for distinguishing in the UI.
    // TODO: It's currently kinda silly. The ideal solution would be distinguishing by type (which directly relates to restrictions) + Origin.
    // Making OverTorsoStorage and TorsoPockets no longer necessary.
    public enum Type
    {
        General,    // Any general container
        Body,       // A container for a body, for placing stuff on a body. This includes stuff put into clothes (e.g. on suit storage)
        Pockets,    // Pockets are different from the above purely because they are displayed in the hotbar
        Interactors // Interacting tools (e.g. hands) which can also hold items
    }

    // TODO: SlotType doesn't need to be like this
    public enum SlotType
    {
        General,
        // Body-type stuff
        Head,
        Eyes,
        Mouth,
        Ear,
        Torso,
        OverTorso,
        Hands,
        Feet,
        // Other kinda on-body stuff
        OverTorsoStorage,
        // Pockets
        LeftPocket,
        RightPocket,
        // Interactor stuff
        LeftHand,
        RightHand
    }

    public static bool AreCompatible(SlotType slot, Item.ItemType item)
    {
        // This is somewhat hacky, but slots are potentially subject to change anyway.
        return slot == SlotType.General || (int)slot == (int)item || (int)slot > 9;
    }

    public delegate void OnChange(IReadOnlyList<Item> items);
    #endregion

    // Editor properties
    public string containerName;
    public Type containerType;
    [SerializeField]
    protected SlotType[] slots;
    // [SerializeField] private bool recursive; // Whether the container will try and add child containers

    // TODO: Network

    // Called whenever items in the container change.
    public event OnChange onChange;
    // The owner of this container. Most of the time this is just the object itself.
    public GameObject owner;

    /**
     * Add an item to a specific slot
     */
    public virtual void AddItem(int slot, Item item)
    {
        if (items[slot] != null)
            throw new Exception("Item already exists in slot"); // TODO: Specific exception

        items[slot] = item;

        onChange?.Invoke(Array.AsReadOnly(items));
    }
    /**
     * Add an item to the first available slot.
     * Returns the slot it was added to. If item could not be added, -1 is returned.
     * Note: Will call AddItem(slot, item) if a slot is found
     */
    public int AddItem(Item item)
    {
        for (int i = 0; i < items.Length; ++i)
        {
            if(items[i] == null && AreCompatible(slots[i], item.itemType))
            {
                AddItem(i, item);
                return i;
            }
        }

        return -1;
    }
    /**
     * Remove the item from the container, returning the Item.
     */
    public virtual Item RemoveItem(int slot)
    {
        if (items[slot] == null)
            throw new Exception("No item exists in slot"); // TODO: Specific exception

        var item = items[slot];
        items[slot] = null;

        onChange?.Invoke(Array.AsReadOnly(items));

        return item;
    }
    /**
     * Get the item at the given slot
     */
    public Item GetItem(int slot) => items[slot];
    /**
     * Get the slot type of a given slot
     */
    public SlotType GetSlot(int slot) => slots[slot];
    public int Length() => items.Length;

    private void Awake()
    {
        items = new Item[slots.Length];
        owner = gameObject;
    }
    
    // TODO: Share over networking
    private Item[] items = null;
}
