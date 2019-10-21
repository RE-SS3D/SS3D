using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A container holds items with given item slots.
 */
public class Container : MonoBehaviour
{
    /**
     * ItemSlot describes the requirements of a holder of items
     */
    [System.Serializable]
    public struct SlotRequirements
    {
        // The list of restrictions on the item
        public string[] itemRestrictions;
        // The number of items that can be held
        public int      count;
    }


    // What can go in the container
    [SerializeField]
    protected SlotRequirements[] slots;

    // The items that are stored in here.
    // Public to be accessible to others, but should not be determined by editor.
    [System.NonSerialized]
    public Item[] items = null;

    /**
     * Add an item to the container
     */
    public virtual void AddItem(int index, Item item)
    {
        CmdAdd(index, item);
    }

    /**
     * Remove an item from the container
     */
    public virtual void RemoveItem(int index)
    {
        CmdRemove(index);
    }

    /**
     * An Add Item command, to perform the actual change from the server side.
     * This is seperate from AddItem() as a command cannot be virtual
     */
    [Command]
    private void CmdAdd(int index, Item item)
    {
        items[index] = item;
    }

    /**
     * A Remove Item command, to perform the actual change from the server side.
     * This is seperate from RemoveItem() as a command cannot be virtual
     */
    [Command]
    private void CmdRemove(int index)
    {
        items[index] = null;
    }


    private void Start()
    {
        int itemCount = 0;
        foreach(SlotRequirements slot in slots)
            itemCount += slot.count;

        items = new Item[itemCount];
    }
}
