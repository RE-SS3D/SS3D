using System;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory.Extensions;
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

        /// <summary>
        /// The hands used by this inventory
        /// </summary>
        public Hands Hands;
        
        private readonly List<AttachedContainer> openedContainers = new List<AttachedContainer>();
        private float nextAccessCheck;

        public delegate void ContainerEventHandler(AttachedContainer container);

        public event ContainerEventHandler ContainerOpened;
        public event ContainerEventHandler ContainerClosed;

        public void Awake()
        {
            Hands.Inventory = this;
        }

        public void Update()
        {
            float time = Time.time;
            if (time > nextAccessCheck)
            {
                var creature = GetComponent<Creature>();
                for (var i = 0; i < openedContainers.Count; i++)
                {
                    AttachedContainer attachedContainer = openedContainers[i];
                    if (!creature.CanInteract(attachedContainer.gameObject))
                    {
                        RemoveContainer(attachedContainer);
                        i--;
                    }
                }

                nextAccessCheck = time + 0.5f;
            }
        }

        /// <summary>
        /// Interacting with a container that has one "slot"
        /// </summary>
        public void ClientInteractWithSingleSlot(AttachedContainer container)
        {
            // no touchy ;)
            if (Hands == null)
            {
                return;
            }

            
            if (Hands.SelectedHandEmpty)
            {
                if (!container.Container.Empty)
                {
                    ClientTransferItem(container.Container.Items.First(), Vector2Int.zero, Hands.SelectedHand);
                }
            }
            else
            {
                if (container.Container.Empty)
                {
                    ClientTransferItem(Hands.ItemInHand, Vector2Int.zero, container);
                }
                else if (Hands.SelectedHand == container)
                {
                    var handler = GetComponent<InteractionHandler>();
                    if (handler != null)
                    {
                        handler.InteractInHand(Hands.ItemInHand.gameObject, gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Interact with a container at a certain position
        /// </summary>
        /// <param name="container">The container being interacted with</param>
        /// <param name="position">At which position the interaction happened</param>
        public void ClientInteractWithContainerSlot(AttachedContainer container, Vector2Int position)
        {
            if (Hands == null)
            {
                return;
            }

            Item item = container.Container.ItemAt(position);
            if (Hands.SelectedHandEmpty)
            {
                if (item != null)
                {
                    ClientTransferItem(item, Vector2Int.zero, Hands.SelectedHand);
                }
            }
            else
            {
                if (item == null)
                {
                    ClientTransferItem(Hands.ItemInHand, position, container);
                }
            }
        }

        public bool CanModifyContainer(AttachedContainer container)
        {
            // TODO: This root transform check might allow you to take out your own organs down the road O_O
            return openedContainers.Contains(container) || container.transform.root == transform;
        }

        /// <summary>
        /// Requests the server to transfer an item
        /// </summary>
        /// <param name="item">The item to transfer</param>
        /// <param name="targetContainer">Into which container to move the item</param>
        public void ClientTransferItem(Item item, Vector2Int position, AttachedContainer targetContainer)
        {
            NetworkedContainerReference? reference = NetworkedContainerReference.CreateReference(targetContainer);
            if (reference == null)
            {
                Debug.LogError("Couldn't create reference for container in item transfer", targetContainer);
                return;
            }
            
            CmdTransferItem(item.gameObject, position, (NetworkedContainerReference) reference);
        }

        /// <summary>
        /// Requests the server to drop an item out of a container
        /// </summary>
        /// <param name="item">The item to drop</param>
        public void ClientDropItem(Item item)
        {
            CmdDropItem(item.gameObject);
        }

        [Command]
        private void CmdTransferItem(GameObject itemObject, Vector2Int position, NetworkedContainerReference reference)
        {
            var item = itemObject.GetComponent<Item>();
            if (item == null)
            {
                return;
            }

            Container itemContainer = item.Container;
            if (itemContainer == null)
            {
                return;
            }

            AttachedContainer attachedTo = itemContainer.AttachedTo;
            if (attachedTo == null)
            {
                return;
            }

            AttachedContainer attachedContainer = reference.FindContainer();
            if (attachedContainer == null)
            {
                Debug.LogError($"Client sent invalid container reference: NetId {reference.SyncNetworkId}, Container {reference.ContainerIndex}");
                return;
            }

            if (!CanModifyContainer(attachedTo) || !CanModifyContainer(attachedContainer))
            {
                return;
            }

            var creature = GetComponent<Creature>();
            if (creature == null || !creature.CanInteract(attachedContainer.gameObject))
            {
                return;
            }
            
            attachedContainer.Container.AddItem(item, position);
        }

        /// <summary>
        /// Make this inventory open an container
        /// </summary>
        public void OpenContainer(AttachedContainer container)
        {
            container.AddObserver(GetComponent<Creature>());
            openedContainers.Add(container);
            NetworkConnection client = connectionToClient;
            if (client != null)
            {
                TargetOpenContainer(client, container);
            }
        }

        /// <summary>
        /// Removes an container from this inventory
        /// </summary>
        public void RemoveContainer(AttachedContainer container)
        {
            if (openedContainers.Remove(container))
            {
                NetworkConnection client = connectionToClient;
                if (client != null)
                {
                    TargetCloseContainer(client, container);
                }
            }
        }

        [Command]
        public void CmdContainerClose(AttachedContainer container)
        {
            RemoveContainer(container);
        }

        /// <summary>
        /// Does this inventory have a specific container
        /// </summary>
        public bool HasContainer(AttachedContainer container)
        {
            return openedContainers.Contains(container);
        }

        [Command]
        private void CmdDropItem(GameObject gameObject)
        {
            var item = gameObject.GetComponent<Item>();
            if (item == null)
            {
                return;
            }

            AttachedContainer attachedTo = item.Container?.AttachedTo;
            if (attachedTo == null)
            {
                return;
            }

            if (!CanModifyContainer(attachedTo))
            {
                return;
            }

            item.Container = null;
        }

        [TargetRpc]
        private void TargetOpenContainer(NetworkConnection target, AttachedContainer container)
        {
            OnContainerOpened(container);
        }
        
        [TargetRpc]
        private void TargetCloseContainer(NetworkConnection target, AttachedContainer container)
        {
            OnContainerClosed(container);
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

        protected virtual void OnContainerOpened(AttachedContainer container)
        {
            ContainerOpened?.Invoke(container);
        }

        protected virtual void OnContainerClosed(AttachedContainer container)
        {
            ContainerClosed?.Invoke(container);
        }
    }
}