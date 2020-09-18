using Mirror;
using System;
using System.Linq;
using System.Collections.Generic;
using SS3D.Content.Systems.Interactions;
using UnityEngine;
using UnityEngine.Assertions;

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

        public static bool AreCompatible(Filter slot, Item item)
        {
            if (slot == null)
            {
                Debug.LogWarning("Trying to use a container without a filter");
                return true;
            }
            
            Assert.IsNotNull(item);

            return slot.CanStore(item);
        }

        public static bool CanStore(Container container, Item item)
        {
            if (container.volumeLimited && !(container.volume + item.Volume <= container.maxVolume))
                return false;
            if (container.containerFilter == null)
            {
                Debug.LogWarning("Trying to use a container without a filter", container);
                return true;
            }

            return container.containerFilter.CanStore(item);
        }

        public class ItemList : SyncList<GameObject> { }
        #endregion

        // Editor properties
        public string containerName;
        public Filter containerFilter;
        public bool volumeLimited = true;
        public float maxVolume = 50f;
        public int slots;
        protected float volume;

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

            if (!CanStore(this, itemComponent))
                throw new Exception("Item cannot be stored");

            item.SetActive(false);
            items[slot] = item;
            itemComponent.container = this;
            RecalculateVolume(this);
        }
        /**
         * Add an item to the first available slot.
         * Returns the slot it was added to. If item could not be added, -1 is returned.
         * Note: Will call AddItem(slot, item) if a slot is found
         */
        [Server]
        public int AddItem(GameObject item)
        {
            var itemComponent = item.GetComponent<Item>();
            for (int i = 0; i < items.Count; ++i) {
                if (items[i] == null && CanStore(this, itemComponent)) {
                    AddItem(i, item);
                    return i;
                }
            }
            RecalculateVolume(this);
            return -1;
        }

        [Server]
        public void RecalculateVolume(Container container)
        {
            container.volume = 0f;
            for (int i = 0; i < items.Count; ++i)
            {
                var item = container.GetItem(i);
                if (item != null)
                    container.volume += item.Volume;
            }
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

            RecalculateVolume(this);
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
            RecalculateVolume(this);
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
        public virtual Filter GetFilter(int slot) => containerFilter;
        public int Length() => slots;

        public bool IsFilter(string name)
        {
            var hash = Animator.StringToHash(name.ToUpper());
            return containerFilter.Hash == hash;
        }

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
                item.Destroy();
                items[i] = null;
            }
        }
        public override void OnStartServer()
        {
            for (int i = 0; i < slots; ++i)
                items.Add(null);
        }

        readonly private ItemList items;
    }
}
