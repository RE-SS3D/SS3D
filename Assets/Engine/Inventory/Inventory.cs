using System;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using SS3D.Content;
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
                var creature = GetComponent<Entity>();
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
        /// Use it to switch between active hands.
        /// </summary>
        /// <param name="container">This AttachedContainer should be the hand to activate.</param>
        public void ActivateHand(AttachedContainer container)
        {
            Hands.SetActiveHand(container);
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
                    ClientTransferItem(container.Container.Containerizables.First(), Vector2Int.zero, Hands.SelectedHand);
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
                        handler.InteractInHand(Hands.ItemInHand.GetGameObject(), gameObject);
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

            IContainable containable = container.Container.ContainableAt(position);
            if (Hands.SelectedHandEmpty)
            {
                if (containable != null)
                {
                    ClientTransferItem(containable, Vector2Int.zero, Hands.SelectedHand);
                }
            }
            else
            {
                if (containable == null)
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
        /// Requests the server to transfer an containable
        /// </summary>
        /// <param name="containable">The containable to transfer</param>
        /// <param name="targetContainer">Into which container to move the containable</param>
        public void ClientTransferItem(IContainable containable, Vector2Int position, AttachedContainer targetContainer)
        {
            NetworkedContainerReference? reference = NetworkedContainerReference.CreateReference(targetContainer);
            if (reference == null)
            {
                Debug.LogError("Couldn't create reference for container in containable transfer", targetContainer);
                return;
            }
            
            CmdTransferItem(containable.GetGameObject(), position, (NetworkedContainerReference) reference);
        }

        /// <summary>
        /// Requests the server to drop an containable out of a container
        /// </summary>
        /// <param name="containable">The containable to drop</param>
        public void ClientDropItem(IContainable containable)
        {
            CmdDropItem(containable.GetGameObject());
        }

        [Command]
        private void CmdTransferItem(GameObject itemObject, Vector2Int position, NetworkedContainerReference reference)
        {
            var containable = itemObject.GetComponent<IContainable>();
            if (containable == null)
            {
                return;
            }

            Container itemContainer = containable.Container;
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

            var creature = GetComponent<Entity>();
            if (creature == null || !creature.CanInteract(attachedContainer.gameObject))
            {
                return;
            }
            
            attachedContainer.Container.AddContainable(containable, position);
        }

        /// <summary>
        /// Make this inventory open an container
        /// </summary>
        public void OpenContainer(AttachedContainer container)
        {
            container.AddObserver(GetComponent<Entity>());
            openedContainers.Add(container);
            SetOpenState(container, true);
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
                SetOpenState(container, false);
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
            var containable = gameObject.GetComponent<Item>();
            if (containable == null)
            {
                return;
            }

            AttachedContainer attachedTo = containable.Container?.AttachedTo;
            if (attachedTo == null)
            {
                return;
            }

            if (!CanModifyContainer(attachedTo))
            {
                return;
            }

            containable.Container = null;
        }

        [TargetRpc]
        private void TargetOpenContainer(NetworkConnection target, AttachedContainer container)
        {
            OnContainerOpened(container);        
        }

        /// <summary>
        /// On containers having OpenWhenContainerViewed set true, this set the containers state appropriately.
        /// If the container is viewed by another entity, it's already opened, and therefore it does nothing.
        /// If this entity is the first to view it, it trigger the open animation of the object.
        /// If the entity is the last to view it, it closes the container.
        /// </summary>
        /// <param name="container"> The container viewed by this entity.</param>
        /// <param name="state"> The state to set in the container, true is opened and false is closed.</param>
        [Server]
        private void SetOpenState(AttachedContainer container, bool state)
        {
            if (container.containerDescriptor.openWhenContainerViewed)
            {
                Entity currentObserver = GetComponent<Entity>(); 
            foreach (Entity observer in container.Observers)
            {
                // checks if the container is already viewed by another entity
                if (observer.Hands.Inventory.HasContainer(container) && observer != currentObserver)
                {              
                    return;
                }
            }
            container.containerDescriptor.containerInteractive.setOpenState(state);
            }
        }


        [TargetRpc]
        private void TargetCloseContainer(NetworkConnection target, AttachedContainer container)
        {
            OnContainerClosed(container);    
        }

        /**
         * Graphically adds the containable back into the world (for server and all clients).
         * Must be called from server initially
         */
        private void Spawn(GameObject containable, Vector3 position, Quaternion rotation)
        {
            // World will be the parent
            containable.transform.parent = null;

            Vector3 itemDimensions = containable.GetComponentInChildren<Collider>().bounds.size;
            float itemSize = 0;
            
            for(int i = 0; i < 3; i++) {
                if (itemDimensions[i] > itemSize)
                    itemSize = itemDimensions[i];                 
            }
            float distance = Vector3.Distance(containable.transform.position, position);
            position = distance > 0 ? position + new Vector3(0, itemSize * 0.5f, 0) : position;

            if (distance > 0)
                containable.transform.LookAt(transform);
            else
                containable.transform.rotation = rotation;
            containable.transform.position = position;
            //containable.transform.rotation = containable.GetComponent<Item>().attachmentPoint.rotation;
            
            //Vector3 transformRotation = containable.transform.rotation.eulerAngles;
            //transformRotation.x = 0f;
            //transformRotation.z = 0f;
            //containable.transform.rotation = Quaternion.Euler(transformRotation);
            containable.SetActive(true);

            if (isServer)
                RpcSpawn(containable, position, rotation);
        }

        [ClientRpc]
        private void RpcSpawn(GameObject containable, Vector3 position, Quaternion rotation)
        {
            if (!isServer) // Silly thing to prevent looping when server and client are one
                Spawn(containable, position, rotation);
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