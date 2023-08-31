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
using FishNet.Object.Synchronizing;
using System.ComponentModel;
using static UnityEngine.GraphicsBuffer;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Inventory stores all containers that are visible in slots on the player. That includes clothing, hands, id, backpack and others.
    /// It also handles doing a bunch of moving item around containers on the player and out of it.
    /// </summary>
    public class HumanInventory : NetworkActor
    {
		/// <summary>
		/// List of containers present on the player, meaning, in the player HUD, shown as slots.
		/// </summary>
        [SyncObject]
        private readonly SyncList<AttachedContainer> ContainersOnPlayer = new();


        public delegate void InventoryContainerModifiedEventHandler(AttachedContainer container);
        public delegate void ContainerContentsEventHandler(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type);
        public delegate void Notify();

		// When a container is added to this inventory
        public event InventoryContainerModifiedEventHandler OnInventoryContainerAdded;

		// When a container is removed from this inventory
        public event InventoryContainerModifiedEventHandler OnInventoryContainerRemoved;

		// When the content of a container in this inventory changes
        public event ContainerContentsEventHandler OnContainerContentChanged;

		// When the inventory is done doing its setup
        public event Notify OnInventorySetUp;

		// reference to the component allowing to display out of inventory containers.
        public ContainerViewer containerViewer;

		public List<AttachedContainer> Containers => ContainersOnPlayer.Collection.ToList();

        /// <summary>
        /// The controllable body of the owning player
        /// </summary>
        public Entity Body;

        /// <summary>
        /// The hands used by this inventory
        /// </summary>
        public Hands Hands;

		/// <summary>
		/// Number of hands container on this inventory.
		/// </summary>
		public int CountHands => ContainersOnPlayer.Where(x => x.Type == ContainerType.Hand).Count();

		/// <summary>
		/// Try to get a particular type of container in the inventory, and if there's multiple, try to get the one at the given position.
		/// </summary>
		/// <param name="position">The position of the container for a given type, if there's two pocket containers, it'd be 0 and 1</param>
		/// <param name="type"> The container we want back.</param>
		/// <returns></returns>
		public bool TryGetTypeContainer(ContainerType type, int position, out AttachedContainer typeContainer) 
        {
            int typeIndex = 0;
            foreach (var container in ContainersOnPlayer) 
            {
                if(container.Type == type && position == typeIndex)
                {
                    typeContainer = container;
                    return true;
                }
                else if(container.Type == type)
                {
                    typeIndex++;
                }
            }
            typeContainer = null;
            return false;
        }

        protected override void OnAwake()
        {
            ContainersOnPlayer.OnChange += SyncInventoryContainerChange;
        }

        /// <summary>
        /// Called on server and client whenever there's an operation on the attached Container synclist.
        /// The main role of this callback is to invoke events regarding the change of containers in the inventory for
        /// other scripts to update.
        /// </summary>
        private void SyncInventoryContainerChange(SyncListOperation op, int index, AttachedContainer oldContainer, AttachedContainer newContainer, bool asServer)
        {
            if (asServer) return;
            switch (op)
            {
                case SyncListOperation.Add:
                    OnInventoryContainerAdded?.Invoke(newContainer);
                    break;
                case SyncListOperation.RemoveAt:
                    Debug.Log("remove container " + oldContainer);
                    OnInventoryContainerRemoved?.Invoke(oldContainer);
                    break;
            }
        }

		public override void OnStartClient()
        {
            base.OnStartClient();
			if (!IsOwner)
			{
				return;
			}

            Hands.SetInventory(this);
            SetupView();
			Subsystems.Get<RoleSystem>().GiveRoleLoadoutToPlayer(Body);
			OnInventorySetUp?.Invoke();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            SetUpContainers();
        }

        /// <summary>
        /// Get the attached container on the Human prefab and put them in this inventory.
        /// Add only containers that display as slots in inventory.
        /// </summary>
        [Server]
        private void SetUpContainers()
        {
            var attachedContainers = GetComponentsInChildren<AttachedContainer>().Where(x => x.DisplayAsSlotInUI);
            foreach (var container in attachedContainers)
            {
                AddContainer(container);
                Punpun.Information(this, "Adding {container} container to inventory", Logs.Generic, container);
            }
        }

        [Client]
        private void SetupView()
        {
            var inventoryView = ViewLocator.Get<InventoryView>().First();
            inventoryView.Setup(this);
        }

        /// <summary>
        /// Add a given container to this inventory, and register to a few events related to the container.
        /// Only use this method to remove a container to ContainersOnPlayer.
        /// </summary>
        [Server]
        private void AddContainer(AttachedContainer container)
        {
            ContainersOnPlayer.Add(container);
            container.OnContentsChanged += HandleContainerContentChanged;
            container.OnItemAttached += HandleTryAddContainerOnItemAttached;
            container.OnItemDetached += HandleTryRemoveContainerOnItemDetached;

			// Be careful, destroying an inventory container will cause issue as when syncing with client, the attachedContainer will be null. 
			// Before destroying a container, consider disabling the behaviour or the game object it's on first to avoid this issue.
			container.OnAttachedContainerDisabled += RemoveContainer;

		}

        /// <summary>
        /// Remove a given container to this inventory, and unregister to a few events related to the container.
        /// Only use this method to remove a container to ContainersOnPlayer.
        /// </summary>
        [Server]
        private void RemoveContainer(AttachedContainer container)
        {
            ContainersOnPlayer.Remove(container);
            container.OnContentsChanged -= HandleContainerContentChanged;
            container.OnItemAttached -= HandleTryAddContainerOnItemAttached;
            container.OnItemDetached -= HandleTryRemoveContainerOnItemDetached;
			container.OnAttachedContainerDisabled -= RemoveContainer;
		}

		/// <summary>
		/// Try to add a container to this inventory, check first if not already added.
		/// TODO: Should also check if it's the kind of container that can go in inventory.
		/// </summary>
		[Server]
		public bool TryAddContainer(AttachedContainer container)
		{
			if (!Containers.Contains(container))
			{
				AddContainer(container);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Try to remove a container already present in this inventory.
		/// </summary>
		[Server]
		public bool TryRemoveContainer(AttachedContainer container)
		{
			if (Containers.Contains(container))
			{
				RemoveContainer(container);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Simply invoke the event OnContainerContentChanged.
		/// </summary>
		private void HandleContainerContentChanged(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
            OnContainerContentChanged?.Invoke(container,oldItem,newItem,type);
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
            Hands.CmdSetActiveHand(container);
        }

        [ServerRpc]
        private void CmdDropItem(GameObject gameObject)
        {
            Item item = gameObject.GetComponent<Item>();
            if (item == null)
            {
                return;
            }

            AttachedContainer attachedTo = item.Container;
            if (attachedTo == null)
            {
                return;
            }

            if (!containerViewer.CanModifyContainer(attachedTo))
            {
                return;
            }

            item.SetContainer(null);
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

		[ServerRpc]
        private void CmdTransferItem(GameObject itemObject, Vector2Int position, AttachedContainer container)
        {
            Item item = itemObject.GetComponent<Item>();
            if (item == null)
            {
                return;
            }

            AttachedContainer itemContainer = item.Container;
            if (itemContainer == null)
            {
                return;
            }

            // Can't put an item in its own container
            if (item.GetComponentsInChildren<AttachedContainer>().AsEnumerable().Contains(container)){
				Punpun.Warning(this, "can't put an item in its own container");
                return;
            }

            if (container == null)
            {
                Punpun.Error(this, $"Client sent invalid container reference: NetId {container.ObjectId}");
                return;
            }

            if (!containerViewer.CanModifyContainer(itemContainer) || !containerViewer.CanModifyContainer(container))
            {
                return;
            }

            Hands hands = GetComponent<Hands>();
            if (hands == null || !hands.SelectedHand.CanInteract(container.gameObject))
            {
                return;
            }

            if (!container.CanContainItem(item))
            {
                return;
            }
			if(itemContainer != container)
				itemContainer.RemoveItem(item);

            container.AddItemPosition(item, position);      
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

            Item item = container.ItemAt(position);
            // If selected hand is empty and an item is present on the slot position in the container, transfer it to hand.
            if (Hands.SelectedHand.IsEmpty())
            {
                if (item != null)
                {
                    ClientTransferItem(item, Vector2Int.zero, Hands.SelectedHand.Container);
                }
            }
            // If selected hand has an item and there's no item on the slot in the container, transfer it to container slot.
            else
            {
                if (item == null)
                {
                    ClientTransferItem(Hands.SelectedHand.ItemInHand, position, container);
                }
            }
        }


        public bool HasPermission(IDPermission permission)
        {
            // This check only in the first identification containers, if there's multiple and the id is not in the first one it won't work.
            if(!TryGetTypeContainer(ContainerType.Identification, 0, out AttachedContainer IDContainer))
            {
                return false;
            }

            IIdentification id = IDContainer.Items.FirstOrDefault() as IIdentification;
            if (id == null)
            {
                return false;
            }
            return id.HasPermission(permission);
        }

        /// <summary>
        /// When an item is added to one of the inventory containers, check if this item has some containers that should be displayed by the inventory too.
        /// Add them to it if that's the case. E.g. a jumpsuit with pockets.
        /// </summary>
        private void HandleTryAddContainerOnItemAttached(object sender, Item item)
        {
            var parentContainer = (AttachedContainer)sender;
            var itemContainers = item.GetComponentsInChildren<AttachedContainer>().Where(x=> x.DisplayAsSlotInUI);
            
            foreach (var container in itemContainers)
            {
                if( container.GetComponentInParent<Item>() != item)
                {
                    continue;
                }
                // If the item is held in hand, ignore it, it's not worn by the player so it shouldn't add yet any containers.
                if (parentContainer == null || parentContainer.Type == ContainerType.Hand)
                {
                    continue;
                }

                if (!ContainersOnPlayer.Contains(container))
                {
                    AddContainer(container);
                }
            }    
        }

        /// <summary>
        /// When removing an item from one the inventory containers, check if that item had some containers like pockets part of the inventory,
        /// and remove them too if that's the case.
        /// </summary>
        private void HandleTryRemoveContainerOnItemDetached(object sender, Item item)
        {
            var parentContainer = (AttachedContainer)sender;
            var itemContainers = item.GetComponentsInChildren<AttachedContainer>();
            foreach (var container in itemContainers)
            {
                // If the item is held in hand, ignore it, it's not worn by the player so it shouldn't remove any containers.
                if (parentContainer == null || parentContainer.Type == ContainerType.Hand)
                {
                    continue;
                }

                if (ContainersOnPlayer.Contains(container))
                {
                    RemoveContainer(container);
                    Punpun.Warning(this, $"invoke {container} removed");
                }
            }
        }
	}
}
