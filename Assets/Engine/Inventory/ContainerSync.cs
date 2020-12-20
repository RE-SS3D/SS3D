using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace SS3D.Engine.Inventory
{
    /// <summary>
    /// Syncs all accessible containers on this network object
    /// </summary>
    public class ContainerSync : NetworkBehaviour
    {
        private Container.ContainerContentsHandler[] changeHandlers;

        public List<AttachedContainer> Containers { get; private set; } = new List<AttachedContainer>();

        public void Start()
        {
            UpdateContainers();
        }

        public int IndexOf(AttachedContainer attachedContainer)
        {
            return Containers.IndexOf(attachedContainer);
        }

        /// <summary>
        /// Updates the container list of this instance
        /// <remarks>Make sure you maintain the same state on the server and client!</remarks>
        /// </summary>
        public void UpdateContainers()
        {
            if (changeHandlers != null)
            {
                for (var i = 0; i < Containers.Count; i++)
                {
                    AttachedContainer accessible = Containers[i];
                    accessible.Container.ContentsChanged -= changeHandlers[i];
                }

                changeHandlers = null;
            }
            
            Containers.Clear();
            GetComponentsInChildren(false, Containers);
            if (NetworkServer.active)
            {
                SubscribeToContainers();
            }
        }

        private void SubscribeToContainers()
        {
            // Go through each container, subscribing to events
            changeHandlers = new Container.ContainerContentsHandler[Containers.Count];
            for (var i = 0; i < Containers.Count; i++)
            {
                AttachedContainer accessible = Containers[i];

                // Container contents change
                void ContentsHandler(Container _, IEnumerable<Item> items, Container.ContainerChangeType type)
                {
                    SyncContainerDelta(accessible, items, type);
                }

                accessible.Container.ContentsChanged += ContentsHandler;
                changeHandlers[i] = ContentsHandler;

                // New accessor
                accessible.NewObserver += SyncContainer;
            }
        }

        /// <summary>
        /// Syncs an entire container to a client
        /// </summary>
        /// <param name="container">The container to synchronise</param>
        /// <param name="accessor">The creature to sync to</param>
        private void SyncContainer(AttachedContainer container, Creature creature)
        {
            var identity = creature.GetComponent<NetworkIdentity>();
            if (identity == null)
            {
                return;
            }

            var client = identity.connectionToClient;
            if (client == null)
            {
                return;
            }

            int index = Containers.FindIndex(c => container == c);
            TargetSyncContainer(client, index, container.Container);
        }


        /// <summary>
        /// Syncs a single item change to all accessing clients
        /// </summary>
        /// <param name="container">The container the change happened in</param>
        /// <param name="changedItems">The items that changed</param>
        /// <param name="type">The type of change</param>
        private void SyncContainerDelta(AttachedContainer attachedContainer, IEnumerable<Item> changedItems,
            Container.ContainerChangeType type)
        {
            if (attachedContainer.Observers.Count == 0)
            {
                return;
            }

            int index = Containers.FindIndex(c => attachedContainer == c);;
            Item[] items = changedItems.ToArray();
            GameObject[] itemGameObjects = items.Select(x => x.gameObject).ToArray();
            Container container = attachedContainer.Container;

            Container.StoredItem[] storedItems = null;
            if (type == Container.ContainerChangeType.Add || type == Container.ContainerChangeType.Move)
            {
                storedItems = new Container.StoredItem[items.Length];
                for (var i = 0; i < items.Length; i++)
                {
                    storedItems[i] = container.StoredItems[container.FindItem(items[i])];
                }
            }


            foreach (Creature creature in attachedContainer.Observers)
            {
                if (creature == null)
                {
                    continue;
                }
                
                var identity = creature.GetComponent<NetworkIdentity>();
                if (identity == null)
                {
                    continue;
                }

                var client = identity.connectionToClient;
                if (client == null)
                {
                    continue;
                }

                if (type == Container.ContainerChangeType.Remove)
                {
                    TargetSyncItemsRemove(client, index, itemGameObjects);
                }
                else if (type == Container.ContainerChangeType.Add)
                {
                    TargetSyncItemsAdd(client, index, storedItems);
                }
                else if (type == Container.ContainerChangeType.Move)
                {
                    TargetSyncItemsMove(client, index, storedItems);
                }
            }
        }

        [TargetRpc]
        private void TargetSyncContainer(NetworkConnection target, int containerId, Container container)
        {
            if (NetworkServer.active)
            {
                return;
            }

            Containers[containerId].Container.Reconcile(container);
        }

        [TargetRpc]
        private void TargetSyncItemsAdd(NetworkConnection target, int containerId, Container.StoredItem[] items)
        {
            if (NetworkServer.active)
            {
                return;
            }

            var accessibleContainer = Containers[containerId];
            accessibleContainer.Container.AddItemsUnchecked(items);
        }

        [TargetRpc]
        private void TargetSyncItemsRemove(NetworkConnection target, int containerId, GameObject[] items)
        {
            if (NetworkServer.active)
            {
                return;
            }

            var accessibleContainer = Containers[containerId];
            accessibleContainer.Container.RemoveItems(items.Select(x => x.GetComponent<Item>()).ToArray());
        }

        [TargetRpc]
        private void TargetSyncItemsMove(NetworkConnection target, int containerId, Container.StoredItem[] items)
        {
            if (NetworkServer.active)
            {
                return;
            }

            var accessibleContainer = Containers[containerId];
            accessibleContainer.Container.MoveItemsUnchecked(items);
        }
    }
}