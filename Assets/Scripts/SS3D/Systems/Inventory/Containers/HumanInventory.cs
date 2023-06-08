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

namespace SS3D.Systems.Inventory.Containers
{

    public class HumanInventory : NetworkActor
    {
        [SyncObject]
        public readonly SyncList<AttachedContainer> OnPlayerContainers = new();

        public delegate void InventoryContainerUpdated(AttachedContainer container);
        public delegate void ContainerContentsHandler(Container container, IEnumerable<Item> oldItems, IEnumerable<Item> newItems, ContainerChangeType type);
        public delegate void Notify();

        public event InventoryContainerUpdated OnInventoryContainerAdded;

        public event InventoryContainerUpdated OnInventoryContainerRemoved;

        public event ContainerContentsHandler OnContainerContentChanged;

        public event Notify OnInventorySetUp;

        public ContainerViewer containerViewer;

        /// <summary>
        /// The controllable body of the owning player
        /// </summary>
        public Entity Body;

        /// <summary>
        /// The hands used by this inventory
        /// </summary>
        public Hands Hands;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position">The position of the container for a given type, if there's two pocket containers, it'd be 0 and 1</param>
        /// <param name="typeContainer"> The container we want back.</param>
        /// <returns></returns>
        public bool TryGetTypeContainer(ContainerType type, int position, out AttachedContainer typeContainer) 
        {
            int typeIndex = 0;
            foreach (var container in OnPlayerContainers) 
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
            OnPlayerContainers.OnChange += InventoryContainerOnChange;
        }

        /// <summary>
        /// Called on server and client whenever there's an operation on the attached Container synclist.
        /// </summary>
        private void InventoryContainerOnChange(SyncListOperation op, int index, AttachedContainer oldContainer, AttachedContainer newContainer, bool asServer)
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

        public int CountHands => OnPlayerContainers.Where(x => x.Type == ContainerType.Hand).Count();

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (Owner.IsLocalClient)
            {
                Hands.SetInventory(this);
                SetupView();
                
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            SetUpContainers();
        }

        /// <summary>
        /// Called after OnStartServer, observers are guaranteed to be set up here.
        /// </summary>
        public override void OnSpawnServer(NetworkConnection connection)
        {
            base.OnSpawnServer(connection);
            RpcGiveRoleLoadout(Owner);
            RpcInvokeInventorySetUp(Owner);
        }

        /// <summary>
        /// Get the attached container on the Human prefab and put them in this inventory.
        /// </summary>
        [Server]
        private void SetUpContainers()
        {
            var attachedContainers = GetComponentsInChildren<AttachedContainer>();
            foreach (var container in attachedContainers)
            {
                AddContainer(container);
                Punpun.Information(this, "Adding {container} container to inventory", Logs.Generic, container);
            }
        }

        /// <summary>
        /// This method simply warn the client that everything is set up on the inventory, and things that depends on it can now be called.
        /// </summary>
        /// <param name="conn"></param>
        [TargetRpc]
        private void RpcInvokeInventorySetUp(NetworkConnection conn)
        {
            OnInventorySetUp?.Invoke();
        }


        /// <summary>
        /// Should be called after setting up the inventory. Gives the player its items for its role.
        /// </summary>
        /// <param name="conn"></param>
        [TargetRpc]
        private void RpcGiveRoleLoadout(NetworkConnection conn)
        {
            Subsystems.Get<RoleSystem>().GiveRoleLoadoutToPlayer(Body);
        }

        [Client]
        private void SetupView()
        {
            var inventoryView = ViewLocator.Get<InventoryView>().First();
            inventoryView.Setup(this);
        }

        [Server]
        public void AddContainer(AttachedContainer container)
        {
            OnPlayerContainers.Add(container);
            container.Container.OnContentsChanged += ContainerContentChanged;
            container.OnItemAttached += TryAddContainerOnItemAttached;
            container.OnItemDetached += TryRemoveContainerOnItemDetached;
        }

        [Server]
        public void RemoveContainer(AttachedContainer container)
        {
            OnPlayerContainers.Remove(container);
            container.Container.OnContentsChanged -= ContainerContentChanged;
            container.OnItemAttached -= TryAddContainerOnItemAttached;
            container.OnItemDetached -= TryRemoveContainerOnItemDetached;
        }

        public void ContainerContentChanged(Container container, IEnumerable<Item> oldItems, IEnumerable<Item> newItems, ContainerChangeType type)
        {
            OnContainerContentChanged?.Invoke(container,oldItems,newItems,type);
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
            Hands.SetActiveHand(container);
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

            // Can't put an item in its own container
            if (item.GetComponentsInChildren<AttachedContainer>().AsEnumerable().Contains(container)){
                return;
            }

            if (container == null)
            {
                Debug.LogError($"Client sent invalid container reference: NetId {container.ObjectId}");
                return;
            }

            if (!containerViewer.CanModifyContainer(attachedTo) || !containerViewer.CanModifyContainer(container))
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

        private void TryAddContainerOnItemAttached(object sender, Item item)
        {
            var parentContainer = (AttachedContainer)sender;
            var itemContainers = item.GetComponentsInChildren<InventorySlotContainer>();
            
            foreach (var container in itemContainers)
            {
                if( container.GetComponentInParent<Item>() != item)
                {
                    continue;
                }
                if (parentContainer == null || parentContainer.Type == ContainerType.Hand)
                {
                    continue;
                }

                if (!OnPlayerContainers.Contains(container))
                {
                    AddContainer(container);
                    Punpun.Warning(this, $"invoke {container} added");
                }
            }    
        }

        private void TryRemoveContainerOnItemDetached(object sender, Item item)
        {
            var parentContainer = (AttachedContainer)sender;
            var itemContainers = item.GetComponentsInChildren<AttachedContainer>();
            foreach (var container in itemContainers)
            {
                if (parentContainer == null || parentContainer.Type == ContainerType.Hand)
                {
                    continue;
                }

                if (OnPlayerContainers.Contains(container))
                {
                    RemoveContainer(container);
                    Punpun.Warning(this, $"invoke {container} removed");
                }
            }
        }
    }
}
