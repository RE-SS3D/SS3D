using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Storage.Items;
using SS3D.Systems.Entities;
using SS3D.Systems.Storage.Items;
using UnityEngine;

namespace SS3D.Storage.Containers
{
    /// <summary>
    /// Syncs all accessible containers on this network object
    /// </summary>
    public class ContainerSync : NetworkBehaviour
    {
        private Container.ContainerContentsHandler[] _changeHandlers;

        public List<AttachedContainer> Containers { get; private set; } = new();

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
            if (_changeHandlers != null)
            {
                for (int i = 0; i < Containers.Count; i++)
                {
                    AttachedContainer accessible = Containers[i];
                    accessible.Container.ContentsChanged -= _changeHandlers[i];
                }

                _changeHandlers = null;
            }
            
            Containers.Clear();
            Containers = GetComponentsInChildren<AttachedContainer>(false).ToList();

            SubscribeToContainers();
        }

        private void SubscribeToContainers()
        {
            // Go through each container, subscribing to events
            _changeHandlers = new Container.ContainerContentsHandler[Containers.Count];
            for (int i = 0; i < Containers.Count; i++)
            {
                AttachedContainer accessible = Containers[i];

                // Container contents change
                void contentsHandler(Container _, IEnumerable<Item> items, ContainerChangeType type)
                {
                    SyncContainerDelta(accessible, items, type);
                }

                accessible.Container.ContentsChanged += contentsHandler;
                _changeHandlers[i] = contentsHandler;

                // New accessor
                accessible.NewObserver += SyncContainer;
            }
        }

        /// <summary>
        /// Syncs an entire container to a client
        /// </summary>
        /// <param name="container">The container to synchronise</param>
        /// <param name="accessor">The creature to sync to</param>
        private void SyncContainer(AttachedContainer container, PlayerControllable controllable)
        {
            NetworkObject identity = controllable.GetComponent<NetworkObject>();
            if (identity == null)
            {
                return;
            }

            NetworkConnection client = identity.Owner;
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
            ContainerChangeType type)
        {
            if (attachedContainer.ObservingPlayers.Count == 0)
            {
                return;
            }

            int index = Containers.FindIndex(c => attachedContainer == c);;
            Item[] items = changedItems.ToArray();
            GameObject[] itemGameObjects = items.Select(x => x.gameObject).ToArray();
            Container container = attachedContainer.Container;

            Container.StoredItem[] storedItems = null;
            if (type is ContainerChangeType.Add or ContainerChangeType.Move)
            {
                storedItems = new Container.StoredItem[items.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    storedItems[i] = container.StoredItems[container.FindItem(items[i])];
                }
            }


            foreach (PlayerControllable creature in attachedContainer.ObservingPlayers)
            {
                if (creature == null)
                {
                    continue;
                }
                
                NetworkObject identity = creature.GetComponent<NetworkObject>();
                if (identity == null)
                {
                    continue;
                }

                NetworkConnection client = identity.Owner;
                if (client == null)
                {
                    continue;
                }

                switch (type)
                {
                    case ContainerChangeType.Remove:
                        TargetSyncItemsRemove(client, index, itemGameObjects);
                        break;
                    case ContainerChangeType.Add:
                        TargetSyncItemsAdd(client, index, storedItems);
                        break;
                    case ContainerChangeType.Move:
                        TargetSyncItemsMove(client, index, storedItems);
                        break;
                }
            }
        }

        [TargetRpc]
        private void TargetSyncContainer(NetworkConnection target, int containerId, Container container)
        {
            if (!IsServer)
            {
                return;
            }

            // This prevents handler errors when TargetSyncContainer is called before the Start() method
            // is executed. For example, this can occur for the player character on a client.
            if (Containers.Count == 0)
            {
                UpdateContainers();
            }

            Containers[containerId].Container.Reconcile(container);
        }

        [TargetRpc]
        private void TargetSyncItemsAdd(NetworkConnection target, int containerId, Container.StoredItem[] items)
        {
            if (!IsServer)
            {
                return;
            }

            AttachedContainer accessibleContainer = Containers[containerId];
            accessibleContainer.Container.AddItemsUnchecked(items);
        }

        [TargetRpc]
        private void TargetSyncItemsRemove(NetworkConnection target, int containerId, GameObject[] items)
        {
            if (!IsServer)
            {
                return;
            }

            AttachedContainer accessibleContainer = Containers[containerId];
            accessibleContainer.Container.RemoveItems(items.Select(x => x.GetComponent<Item>()).ToArray());
        }

        [TargetRpc]
        private void TargetSyncItemsMove(NetworkConnection target, int containerId, Container.StoredItem[] items)
        {
            if (!IsServer)
            {
                return;
            }

            AttachedContainer accessibleContainer = Containers[containerId];
            accessibleContainer.Container.MoveItemsUnchecked(items);
        }

        protected override void OnValidate()
        { 
            base.OnValidate();

            RemoveUselessContainerSync(this.gameObject);
        }  

        private void RemoveUselessContainerSync(GameObject gameObject)
        {
            #if UNITY_EDITOR
            ContainerSync containerSync = gameObject.GetComponent<ContainerSync>();
            Transform[] children = gameObject.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child == gameObject.transform)
                    continue;

                ContainerSync containerSyncToRemove = child.GetComponent<ContainerSync>();
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (!Application.isEditor || containerSync == null || containerSyncToRemove == null)
                    {
                        return;
                    }

                    Debug.Log("On object " + child.gameObject.name + ", remove containerSync because a containerSync script is already on a parent");
                    DestroyImmediate(containerSyncToRemove, true);
                };
                     

                RemoveUselessContainerSync(child.gameObject);
            }
            #endif
        }
    }
}