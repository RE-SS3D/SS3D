using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Inventory
{
    /**
     * This is the basic inventory system. Any inventory-capable creature should have this component.
     * The basic inventory system has to handle:
     *  - Aggregating all containers on the player and accessible to the player
     *  - The moving of items from one item-slot to another
    */
    public class Inventory : NetworkBehaviour
    {
        public class InventoryOperationException : System.Exception
        {
            public InventoryOperationException()
            {
            }

            public InventoryOperationException(string message)
                : base(message)
            {
            }
        }

        public struct SlotReference
        {
            public SlotReference(Container container, int slotIndex)
            {
                this.container = container;
                this.slotIndex = slotIndex;
            }

            public Container container;
            public int slotIndex;
        }

        private class GameObjectList : SyncList<GameObject>
        {
        }

        // Called whenever the containers change
        public event GameObjectList.SyncListChanged EventOnChange
        {
            add { objectSources.Callback += value; }
            remove { objectSources.Callback -= value; }
        }

        // The slot the player currently has selected. May be null (container will be null, slotindex will be -1)
        // Note: NOT SYNCHRONIZED. LOCAL PLAYER ONLY
        public SlotReference holdingSlot = new SlotReference(null, -1);

        /**
         * Adds a container source.
         */
        [Server]
        public void AddContainer(GameObject containerObject)
        {
            objectSources.Add(containerObject);
        }

        /**
         * Removes a container source
         */
        [Server]
        public void RemoveContainer(GameObject containerObject)
        {
            objectSources.Remove(containerObject);
        }

        public bool HasContainer(GameObject containerObject) => objectSources.Contains(containerObject);

        /**
         * Add an item from the world into a container.
         */
        [Server]
        public void AddItem(GameObject item, GameObject toContainer, int toIndex)
        {
            Container container = toContainer.GetComponent<Container>();
            Item itemComponent = item.GetComponent<Item>();
            if (container.containerFilter.CanStore(itemComponent))
            {
                Despawn(item);
                container.AddItem(toIndex, item);
            }
        }
        [Server]
        public void AddItem(GameObject item, GameObject toContainer)
        {
            Container container = toContainer.GetComponent<Container>();
            Item itemComponent = item.GetComponent<Item>();
            if (container.containerFilter.CanStore(itemComponent))
            {
                Despawn(item);
                container.AddItem(item);
            }
        }

        /**
         * Place an item from a container into the world.
         */
        [Server]
        public void PlaceItem(GameObject fromContainer, int fromIndex, Vector3 location, Quaternion rotation)
        {
            GameObject item = fromContainer.GetComponent<Container>().RemoveItem(fromIndex);
            Spawn(item, location, rotation);
        }

        /**
         * Destroy an item in the container
         */
        [Server]
        public void DestroyItem(GameObject fromContainer, int fromIndex)
        {
            GameObject item = fromContainer.GetComponent<Container>().RemoveItem(fromIndex);
            Despawn(item);
            Destroy(item);
        }

        /**
         * Move an item from one container to another.
         * This is intended to be called by the UI, when the user drags an item from one place to another
         */
        [Server]
        public void MoveItem(GameObject fromContainer, int fromIndex, GameObject toContainer, int toIndex)
        {
            var from = fromContainer.GetComponent<Container>();
            var to = toContainer.GetComponent<Container>();

            if (!Container.AreCompatible(to.GetFilter(toIndex), from.GetItem(fromIndex)))
                throw new InventoryOperationException("Item not compatible with slot");

            GameObject item = from.RemoveItem(fromIndex);
            to.AddItem(toIndex, item);
        }

        /**
         * Move an item from one container to the default position at another.
         */
        [Server]
        public void MoveItem(GameObject fromContainer, int fromIndex, GameObject toContainer)
        {
            var from = fromContainer.GetComponent<Container>();
            var to = toContainer.GetComponent<Container>();

            GameObject item = from.RemoveItem(fromIndex);
            int itemIndex = to.AddItem(item);

            // If we couldn't add the item, Put it back
            if (itemIndex == -1)
                from.AddItem(fromIndex, item);
        }

        // Note: You need a good reason to call ANY of these.
        //       Currently, only the UIInventory calls these.

        [Command]
        public void CmdAddItem(GameObject item, GameObject toContainer, int toIndex) => AddItem(item, toContainer, toIndex);
        [Command]
        public void CmdAddItemToDefault(GameObject item, GameObject toContainer) => AddItem(item, toContainer);
        [Command]
        public void CmdPlaceItem(GameObject fromContainer, int fromIndex, Vector3 location, Quaternion rotation) => PlaceItem(fromContainer, fromIndex, location, rotation);
        [Command]
        public void CmdMoveItem(GameObject fromContainer, int fromIndex, GameObject toContainer, int toIndex) => MoveItem(fromContainer, fromIndex, toContainer, toIndex);
        [Command]
        public void CmdDestroyItem(GameObject fromContainer, int fromIndex) => DestroyItem(fromContainer, fromIndex);

        public List<Container> GetContainers()
        {
            List<Container> containers = new List<Container>();

            foreach (var obj in objectSources)
            {
                if (obj == null)
                    Debug.Log(
                        "Still have that mirror bug where transmitting self in OnStartServer for some reason doesnt fucking work");
                else
				{
					/* Checks whether the container is already listed before adding it. This is done because mobile inventories (such
					   as the medkit or toolbox) are returned twice while they are held by a player (once as a child of the player, once
					   in thier own right). This was previously causing errors with duplicate Dictionary entries and doubled UI. */
					var objContainers = obj.GetComponentsInChildren<Container>();
					foreach (Container subordinateContainer in objContainers)
					{
						if (!containers.Contains(subordinateContainer))
						{
							containers.Add(subordinateContainer);
						}
					}					
				}
            }

            return containers;
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            CmdAddSelf();
        }

        /**
         * Sets up the containers. Must run on server.
         * Only called in OnStartLocalPlayer. If I try to run the AddSelf code directly
         * in OnStartServer the thing has a fucking tantrum and just adds null to the objectSources list.
         */
        [Command]
        private void CmdAddSelf()
        {
            objectSources.Add(gameObject);
        }

        /**
         * Graphically removes the object from the world (for server and all clients).
         * Must be called from server initially
         */
        private void Despawn(GameObject item)
        {
            item.SetActive(false);

            if (isServer)
                RpcDespawn(item);
        }

        [ClientRpc]
        private void RpcDespawn(GameObject item)
        {
            if (!isServer) // Prevent server double-dipping
                Despawn(item);
        }

        /**
         * Graphically adds the item back into the world (for server and all clients).
         * Must be called from server initially
         */
        private void Spawn(GameObject item, Vector3 position, Quaternion rotation)
        {
            // World will be the parent
            item.transform.parent = null;

            Vector3 itemDimensions = item.GetComponentInChildren<Collider>().bounds.size;
            float itemSize = 0;
            
            for(int i = 0; i < 3; i++) {
                if (itemDimensions[i] > itemSize)
                    itemSize = itemDimensions[i];                 
            }
            float distance = Vector3.Distance(item.transform.position, position);
            position = distance > 0 ? position + new Vector3(0, itemSize * 0.5f, 0) : position;

            if (distance > 0)
                item.transform.LookAt(transform);
            else
                item.transform.rotation = rotation;
            item.transform.position = position;
            //item.transform.rotation = item.GetComponent<Item>().attachmentPoint.rotation;
            
            //Vector3 transformRotation = item.transform.rotation.eulerAngles;
            //transformRotation.x = 0f;
            //transformRotation.z = 0f;
            //item.transform.rotation = Quaternion.Euler(transformRotation);
            item.SetActive(true);

            if (isServer)
                RpcSpawn(item, position, rotation);
        }

        [ClientRpc]
        private void RpcSpawn(GameObject item, Vector3 position, Quaternion rotation)
        {
            if (!isServer) // Silly thing to prevent looping when server and client are one
                Spawn(item, position, rotation);
        }

        // All objects containing containers usable by this player
        private readonly GameObjectList objectSources = new GameObjectList();
    }
}