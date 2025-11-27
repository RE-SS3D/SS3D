using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.UI;
using SS3D.Systems.Roles;
using SS3D.Traits;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Inventory stores all containers that are visible in slots on the player. That includes clothing, hands, id, backpack and others.
    /// It also handles doing a bunch of moving item around containers on the player and out of it.
    /// </summary>
    public class HumanInventory : NetworkActor, IInventory, IIDPermissionProvider
    {
        public delegate void InventoryContainerModifiedEventHandler(AttachedContainer container);

        public delegate void Notify();

        // When a container is added to this inventory
        public event InventoryContainerModifiedEventHandler OnInventoryContainerAdded;

        // When a container is removed from this inventory
        public event InventoryContainerModifiedEventHandler OnInventoryContainerRemoved;

        // When the content of a container in this inventory changes
        public event IInventory.ContainerContentsEventHandler OnContainerContentChanged;

        // When the inventory is done doing its setup
        public event Notify OnInventorySetUp;

        /// <summary>
        /// List of containers present on the player, meaning, in the player HUD, shown as slots.
        /// </summary>
        [SyncObject]
        private readonly SyncList<AttachedContainer> _containersOnPlayer = new();

        /// <summary>
        /// The hands used by this inventory
        /// </summary>
        [FormerlySerializedAs("Hands")]
        [SerializeField]
        private Hands _hands;

        public List<AttachedContainer> Containers => _containersOnPlayer.Collection.ToList();

        public Hands Hands => _hands;

        /// <summary>
        /// Try to get a particular type of container in the inventory, and if there's multiple, try to get the one at the given position.
        /// </summary>
        /// <param name="position">The position of the container for a given type, if there's two pocket containers, it'd be 0 and 1</param>
        /// <param name="type"> The container we want back.</param>
        /// <returns></returns>
        public bool TryGetTypeContainer(ContainerType type, int position, out AttachedContainer typeContainer)
        {
            int typeIndex = 0;

            foreach (AttachedContainer container in _containersOnPlayer)
            {
                if (container.Type == type && position == typeIndex)
                {
                    typeContainer = container;

                    return true;
                }
                else if (container.Type == type)
                {
                    typeIndex++;
                }
            }

            typeContainer = null;

            return false;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                return;
            }

            _hands.SetInventory(this);
            SetupView();
        }

        public void Init()
        {
            OnInventorySetUp?.Invoke();
            RpcInventorySetup();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            SetUpContainers();
        }

        /// <summary>
        /// Interact with a container at a certain position. Transfer items from selected hand to container, or from container to selected hand.
        /// </summary>
        /// <param name="container">The container being interacted with.</param>
        /// <param name="position">Position of the slot where the interaction happened.</param>
        public void ClientInteractWithContainerSlot(AttachedContainer container, Vector2Int position)
        {
            if (_hands == null)
            {
                return;
            }

            Item item = container.ItemAt(position);

            if (container.ContainerType == ContainerType.Hand)
            {
                ActivateHand(container);
            }

            if (container == _hands.SelectedHand.Container && item != null)
            {
                GetComponent<InteractionController>().InteractInHand(item.gameObject, _hands.SelectedHand.gameObject);

                return;
            }

            // If selected hand is empty and an item is present on the slot position in the container, transfer it to hand.
            if (_hands.SelectedHand.IsEmpty())
            {
                if (item != null)
                {
                    ClientTransferItem(item, Vector2Int.zero, _hands.SelectedHand.Container);
                }
            }

            // If selected hand has an item and there's no item on the slot in the container, transfer it to container slot.
            else if (item == null)
            {
                ClientTransferItem(_hands.SelectedHand.ItemHeld, position, container);
            }
        }

        public bool HasPermission(IDPermission permission)
        {
            // This check only in the first identification containers, if there's multiple and the id is not in the first one it won't work.
            if (!TryGetTypeContainer(ContainerType.Identification, 0, out AttachedContainer idContainer))
            {
                return false;
            }

            if (idContainer.Items.FirstOrDefault() is not IIdentification id)
            {
                return false;
            }

            return id.HasPermission(permission);
        }

        /// <summary>
        /// Try to add a container to this inventory, check first if not already added.
        /// TODO: Should also check if it's the kind of container that can go in inventory.
        /// </summary>
        [Server]
        public void AddContainer(AttachedContainer container)
        {
            if (Containers.Contains(container))
            {
                return;
            }

            _containersOnPlayer.Add(container);
            container.OnServerContentsChanged += HandleContainerContentChanged;

            // Be careful, destroying an inventory container will cause issue as when syncing with client, the attachedContainer will be null.
            // Before destroying a container, consider disabling the behaviour or the game object it's on first to avoid this issue.
            // container.OnAttachedContainerDisabled += RemoveContainer;
        }

        /// <summary>
        /// Try to remove a container already present in this inventory.
        /// </summary>
        [Server]
        public void RemoveContainer(AttachedContainer container)
        {
            if (!Containers.Contains(container))
            {
                return;
            }

            _containersOnPlayer.Remove(container);
            container.OnServerContentsChanged -= HandleContainerContentChanged;
        }

        /// <summary>
        /// Requests the server to drop an item out of a container
        /// </summary>
        /// <param name="item">The item to drop</param>
        public void ClientDropItem(Item item)
        {
            CmdDropItem(item.gameObject);
        }

        /// <summary>
        /// Use it to switch between active hands.
        /// </summary>
        /// <param name="container">This AttachedContainer should be the hand to activate.</param>
        public void ActivateHand(AttachedContainer container)
        {
            _hands.CmdSetActiveHand(container);
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

        protected override void OnAwake()
        {
            _containersOnPlayer.OnChange += SyncInventoryContainerChange;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            if (!IsOwner)
            {
                return;
            }

            InventoryView inventoryView = ViewLocator.Get<InventoryView>()[0];
            inventoryView.DestroyAllSlots();
        }

        /// <summary>
        /// Called on server and client whenever there's an operation on the attached Container synclist.
        /// The main role of this callback is to invoke events regarding the change of containers in the inventory for
        /// other scripts to update.
        /// </summary>
        private void SyncInventoryContainerChange(SyncListOperation op, int index, AttachedContainer oldContainer, AttachedContainer newContainer, bool asServer)
        {
            if (asServer)
            {
                return;
            }

            switch (op)
            {
                case SyncListOperation.Add:
                {
                    OnInventoryContainerAdded?.Invoke(newContainer);

                    break;
                }

                case SyncListOperation.RemoveAt:
                {
                    OnInventoryContainerRemoved?.Invoke(oldContainer);

                    break;
                }
            }
        }

        [ObserversRpc]
        private void RpcInventorySetup()
        {
            OnInventorySetUp?.Invoke();
        }

        /// <summary>
        /// Simply invoke the event OnContainerContentChanged.
        /// </summary>
        private void HandleContainerContentChanged(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
            if (type == ContainerChangeType.Add)
            {
               HandleTryAddContainerOnItemAttached(container, newItem);
            }

            if (type == ContainerChangeType.Remove)
            {
                HandleTryRemoveContainerOnItemDetached(container, oldItem);
            }

            OnContainerContentChanged?.Invoke(container, oldItem, newItem, type);
        }

        /// <summary>
        /// Get the attached container on the Human prefab and put them in this inventory.
        /// Add only containers that display as slots in inventory.
        /// </summary>
        [Server]
        private void SetUpContainers()
        {
            IEnumerable<AttachedContainer> attachedContainers = GetComponentsInChildren<InventorySlotContainer>();

            foreach (AttachedContainer container in attachedContainers)
            {
                AddContainer(container);
                Log.Information(this, "Adding {container} container to inventory", Logs.Generic, container);
            }
        }

        [Client]
        private void SetupView()
        {
            InventoryView inventoryView = ViewLocator.Get<InventoryView>()[0];
            inventoryView.Setup(this);
        }

        [ServerRpc]
        private void CmdDropItem(GameObject itemObject)
        {
            if (!itemObject.TryGetComponent(out Item item))
            {
                return;
            }

            AttachedContainer attachedTo = item.Container;

            if (attachedTo == null)
            {
                return;
            }

            attachedTo.RemoveItem(item);
        }

        [ServerRpc]
        private void CmdTransferItem(GameObject itemObject, Vector2Int position, AttachedContainer container)
        {
            if (!itemObject.TryGetComponent(out Item item))
            {
                return;
            }

            AttachedContainer itemContainer = item.Container;

            if (itemContainer == null)
            {
                return;
            }

            // Can't put an item in its own container
            if (item.GetComponentsInChildren<AttachedContainer>().AsEnumerable().Contains(container))
            {
                Log.Warning(this, "can't put an item in its own container");

                return;
            }

            if (container == null)
            {
                Log.Error(this, $"Client sent invalid container reference: NetId {container.ObjectId}");

                return;
            }

            Hands hands = GetComponent<Hands>();

            if (hands == null || !hands.SelectedHand.CanInteract(container.gameObject))
            {
                return;
            }

            itemContainer.TransferItemToOther(item, position, container);
        }

        /// <summary>
        /// When an item is added to one of the inventory containers, check if this item has some containers that should be displayed by the inventory too.
        /// Add them to it if that's the case. E.g. a jumpsuit with pockets.
        /// </summary>
        private void HandleTryAddContainerOnItemAttached(AttachedContainer parentContainer, Item item)
        {
            IEnumerable<InventorySlotContainer> itemContainers = item.GetComponentsInChildren<InventorySlotContainer>();

            foreach (InventorySlotContainer container in itemContainers)
            {
                if (container.GetComponentInParent<Item>() != item)
                {
                    continue;
                }

                // If the item is held in hand, ignore it, it's not worn by the player so it shouldn't add yet any containers.
                if (parentContainer == null || parentContainer.Type == ContainerType.Hand)
                {
                    continue;
                }

                if (!_containersOnPlayer.Contains(container))
                {
                    AddContainer(container);
                }
            }
        }

        /// <summary>
        /// When removing an item from one the inventory containers, check if that item had some containers like pockets part of the inventory,
        /// and remove them too if that's the case.
        /// </summary>
        private void HandleTryRemoveContainerOnItemDetached(AttachedContainer parentContainer, Item item)
        {
            // If the item is held in hand, ignore it, it's not worn by the player so it shouldn't remove any containers.
            if (!parentContainer || parentContainer.Type == ContainerType.Hand)
            {
                return;
            }

            AttachedContainer[] itemContainers = item.GetComponentsInChildren<AttachedContainer>();

            foreach (AttachedContainer container in itemContainers.Where(container => _containersOnPlayer.Contains(container)))
            {
                RemoveContainer(container);
                Log.Warning(this, $"invoke {container} removed");
            }
        }
    }
}
