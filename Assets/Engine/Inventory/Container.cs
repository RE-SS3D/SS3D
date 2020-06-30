using Mirror;
using System;
using System.Linq;
using System.Collections.Generic;
using SS3D.Content.Systems.Interactions;
using UnityEngine;

namespace SS3D.Engine.Inventory
{

    /**
     * A container holds items of a given kind.
     * Attach it to an object to give that object the ability to contain.
     * 
     * Note: 1 security vulnerability: Any client can subscribe to the container and view its contents (though NOT modify), regardless of distance or status
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

        public class ItemList : SyncList<GameObject> { }
        #endregion

        // Editor properties
        public string containerName;
        public Type containerType;
        [SerializeField]
        protected SlotType[] slots;

        public Container()
        {
            items = new ItemList();
        }

        // Called whenever items in the container change.
        public event SyncList<GameObject>.SyncListChanged onChange
        {
            add
            {
                items.Callback += value;
            }
            remove
            {
                items.Callback -= value;
            }
        }

        /**
         * Add an item to a specific slot
         */
        [Server]
        public virtual void AddItem(int slot, GameObject item)
        {
            var itemComponent = item.GetComponent<Item>();
            
            if (itemComponent == null || item.GetComponent<Rigidbody>() == null ||
                item.GetComponent<Collider>() == null)
            {
                return;
            }
            
            if (items[slot] != null)
                throw new Exception("Item already exists in slot"); // TODO: Specific exception

            items[slot] = item;
            itemComponent.container = this;
        }
        /**
         * Add an item to the first available slot.
         * Returns the slot it was added to. If item could not be added, -1 is returned.
         * Note: Will call AddItem(slot, item) if a slot is found
         */
        [Server]
        public int AddItem(GameObject item)
        {
            item.SetActive(false);
            var itemComponent = item.GetComponent<Item>();
            for (int i = 0; i < items.Count; ++i) {
                if (items[i] == null && AreCompatible(slots[i], itemComponent.itemType)) {
                    AddItem(i, item);
                    return i;
                }
            }

            return -1;
        }
        /**
         * Remove the item from the container, returning the Item.
         */
        [Server]
        public virtual GameObject RemoveItem(int slot)
        {
            if (items[slot] == null)
                throw new Exception("No item exists in slot"); // TODO: Specific exception

            var item = items[slot];
            item.GetComponent<Item>().container = null;
            items[slot] = null;

            return item;
        }

        /**
         * Remove the item from the container, returning the Item.
         */
        public void RemoveItem(GameObject item)
        {
            for (var i = 0; i < items.Count; i++)
                if (items[i] == item)
                    RemoveItem(i);
        }

        /**
         * Get all items
         */
        public List<Item> GetItems() => items.Select(i => i?.GetComponent<Item>()).ToList();
        /**
         * Get the item at the given slot
         */
        public Item GetItem(int slot) => items[slot]?.GetComponent<Item>();
        /**
         * Get the slot type of a given slot
         */
        public SlotType GetSlot(int slot) => slots[slot];
        public int Length() => slots.Length;
        /// <summary>
        /// Returns the slot an item is in
        /// </summary>
        /// <param name="item"></param>
        /// <returns>The slot the item is in or -1 if it is not present</returns>
        public int GetSlotFromItem(Item item)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] == item.gameObject)
                {
                    return i;
                }
            }

            return -1;
        }

         /// <summary>
        /// Destroys all items inside this container
        /// </summary>
        public void Purge()
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i].GetComponent<Item>();
                if (item != null)
                {
                    item.container = null;
                }
                ItemHelpers.DestroyItem(item);
                items[i] = null;
            }
        }
        public override void OnStartServer()
        {
            for (int i = 0; i < slots.Length; ++i)
                items.Add(null);
        }

        readonly private ItemList items;
    }
}
