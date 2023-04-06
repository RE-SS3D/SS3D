using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.UI;
using SS3D.Systems.Roles;
using UnityEngine;
using System.Collections;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// This is the basic inventory system. Any inventory-capable creature should have this component.
    /// The basic inventory system has to handle:
    ///  - Aggregating all containers on the player and accessible to the player.
    ///  - The moving of items from one item-slot to another.
    /// </summary>
    public sealed class Inventory : NetworkActor, IIdentification
    {
        public delegate void ContainerEventHandler(AttachedContainer container);

        public event ContainerEventHandler OnContainerOpened;
        public event ContainerEventHandler OnContainerClosed;

        /// <summary>
        /// The hands used by this inventory
        /// </summary>
        public Hands Hands;

        /// <summary>
        /// The container that has the IDCard with permissions
        /// </summary>
        public AttachedContainer IDContainer;

        /// <summary>
        /// The container of the left pocket
        /// </summary>
        public AttachedContainer LeftPocketContainer;

        /// <summary>
        /// The container of the right pocket
        /// </summary>
        public AttachedContainer RightPocketContainer;

        /// <summary>
        /// The controllable body of the owning player
        /// </summary>
        public Entity Body;

        private readonly List<AttachedContainer> _openedContainers = new();

        private float _nextAccessCheck;

        public InventoryView InventoryView { get; private set; }


        public override void OnStartClient()
        {
            base.OnStartClient();

            if(!IsOwner)
            {
                return;
            }

            SetupView();
            Subsystems.Get<RoleSystem>().GiveRoleLoadoutToPlayer(Body);
        }

        private void SetupView()
        {
            InventoryView = ViewLocator.Get<InventoryView>().First();
            InventoryView.Inventory = this;

            InventoryView.Setup();
            InventoryView.Enable(true);
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            AddHandle(UpdateEvent.AddListener(HandleUpdate));

            Hands.Inventory = this;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            InventoryView.Enable(false);
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            float time = Time.time;
            if (!(time > _nextAccessCheck))
            {
                return;
            }

            // Remove all containers from the inventory that can't be interacted with anymore.
            Hands hands = GetComponent<Hands>();
            for (int i = 0; i < _openedContainers.Count; i++)
            {
                AttachedContainer attachedContainer = _openedContainers[i];
                if (hands.CanInteract(attachedContainer.gameObject))
                {
                    continue;
                }

                RemoveContainer(attachedContainer);
                i--;
            }

            _nextAccessCheck = time + 0.5f;
        }

        public bool HasPermission(IDPermission permission)
        {
            IIdentification id = IDContainer.Items.FirstOrDefault() as IIdentification;
            if (id == null)
            {
                return false;
            }

            return id.HasPermission(permission);
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
        /// Interact with a container at a certain position. Transfer items from selected hand to container, or from container to selected hand.
        /// </summary>
        /// <param name="container">The container being interacted with.</param>
        /// <param name="position">Position of the slot where the interaction happened.</param>
        public void ClientInteractWithContainerSlot(AttachedContainer container, Vector2Int position)
        {
            if (Hands == null)
            {
                return;
            }

            Item item = container.Container.ItemAt(position);
            // If selected hand is empty and an item is present on the slot position in the container, transfer it to hand.
            if (Hands.SelectedHandEmpty)
            {
                if (item != null)
                {
                    ClientTransferItem(item, Vector2Int.zero, Hands.SelectedHand);
                }
            }
            // If selected hand has an item and there's no item on the slot in the container, transfer it to container slot.
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
            return _openedContainers.Contains(container) || container.transform.root == transform;
        }

        /// <summary>
        /// Requests the server to transfer an item from one container to another, at the given slot position.
        /// </summary>
        /// <param name="item">The item to transfer</param>
        /// <param name="targetContainer">Into which container to move the item</param>
        public void ClientTransferItem(Item item, Vector2Int position, AttachedContainer targetContainer)
        {
            CmdTransferItem(item.gameObject, position, targetContainer);
        }

        /// <summary>
        /// Requests the server to drop an item out of a container
        /// </summary>
        /// <param name="item">The item to drop</param>
        public void ClientDropItem(Item item)
        {
            CmdDropItem(item.gameObject);
        }

        [ServerRpc]
        private void CmdTransferItem(GameObject itemObject, Vector2Int position, AttachedContainer container)
        {
            Item item = itemObject.GetComponent<Item>();
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

            if (container == null)
            {
                Debug.LogError($"Client sent invalid container reference: NetId {container.ObjectId}");
                return;
            }

            if (!CanModifyContainer(attachedTo) || !CanModifyContainer(container))
            {
                return;
            }

            Hands hands = GetComponent<Hands>();
            if (hands == null || !hands.CanInteract(container.gameObject))
            {
                return;
            }

            itemContainer.RemoveItem(item);
            container.Container.AddItemPosition(item, position);
        }

        /// <summary>
        /// Make this inventory open an container.
        /// </summary>
        public void OpenContainer(AttachedContainer attachedContainer)
        {
            attachedContainer.Container.AddObserver(GetComponent<Entity>());
            _openedContainers.Add(attachedContainer);
            SetOpenState(attachedContainer.gameObject, true);
            NetworkConnection client = Owner;
            if (client != null)
            {
                TargetOpenContainer(client, attachedContainer);
            }
        }

        /// <summary>
        /// Removes a container from this inventory.
        /// </summary>
        public void RemoveContainer(AttachedContainer container)
        {
            container.Container.RemoveObserver(GetComponent<Entity>());
            if (_openedContainers.Remove(container))
            {
                Debug.Log("client call remove");
                SetOpenState(container.gameObject, false);
                NetworkConnection client = Owner;
                if (client != null)
                {
                    TargetCloseContainer(client, container);
                }
            }
        }

        [ServerRpc]
        public void CmdContainerClose(AttachedContainer container)
        {
            RemoveContainer(container);
        }

        /// <summary>
        /// Does this inventory have a specific container ?
        /// </summary>
        public bool HasContainer(AttachedContainer container)
        {
            return _openedContainers.Contains(container);
        }

        [ServerRpc]
        private void CmdDropItem(GameObject gameObject)
        {
            Item item = gameObject.GetComponent<Item>();
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

            item.SetContainer(null);
        }

        [TargetRpc]
        private void TargetOpenContainer(NetworkConnection target, AttachedContainer container)
        {
            InvokeContainerOpened(container);
        }

        /// <summary>
        /// On containers having OpenWhenContainerViewed set true in AttachedContainer, this set the containers state appropriately.
        /// If the container belongs to another Inventory, it's already opened, and therefore it does nothing.
        /// If this Inventory is the first to have it, it triggers the open animation of the object.
        /// If this Inventory is the last to have it, it closes the container.
        /// </summary>
        /// <param name="containerObject"> The container's game object belonging to this inventory.</param>
        /// <param name="state"> The state to set in the container, true is opened and false is closed.</param>
        [Server]
        private void SetOpenState(GameObject containerObject, bool state)
        {
            var container = containerObject.GetComponent<AttachedContainer>();

            if (!container.OpenWhenContainerViewed)
            {
                return;
            }

            Hands hands = GetComponent<Hands>();
            foreach (Entity observer in container.Container.ObservingPlayers)
            {
                // checks if the container is already viewed by another entity
                if (hands.Inventory.HasContainer(container) && observer != hands)
                {
                    return;
                }
            }

            container.ContainerInteractive.SetOpenState(state);
        }


        [TargetRpc]
        private void TargetCloseContainer(NetworkConnection target, AttachedContainer container)
        {
            InvokeContainerClosed(container);
        }


        /// <summary>
        /// Graphically adds the item back into the world(for server and all clients).
        /// Must be called from server initially.
        /// </summary>
        private void Spawn(GameObject item, Vector3 position, Quaternion rotation)
        {
            // World will be the parent
            item.transform.parent = null;

            Vector3 itemDimensions = item.GetComponentInChildren<Collider>().bounds.size;
            float itemSize = 0;

            for (int i = 0; i < 3; i++)
            {
                if (itemDimensions[i] > itemSize)
                {
                    itemSize = itemDimensions[i];
                }
            }

            float distance = Vector3.Distance(item.transform.position, position);
            position = distance > 0 ? position + new Vector3(0, itemSize * 0.5f, 0) : position;

            if (distance > 0)
            {
                item.transform.LookAt(transform);
            }
            else
            {
                item.transform.rotation = rotation;
            }

            item.transform.position = position;
            item.SetActive(true);

            if (IsServer)
            {
                RpcSpawn(item, position, rotation);
            }
        }

        [ObserversRpc]
        private void RpcSpawn(GameObject item, Vector3 position, Quaternion rotation)
        {
            if (!IsServer) // Silly thing to prevent looping when server and client are one
            {
                Spawn(item, position, rotation);
            }
        }

        private void InvokeContainerOpened(AttachedContainer container)
        {
            OnContainerOpened?.Invoke(container);
        }

        private void InvokeContainerClosed(AttachedContainer container)
        {
            OnContainerClosed?.Invoke(container);
        }
    }
}