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

    public class HumanInventory : NetworkActor
    {

        public List<AttachedContainer> OnPlayerContainers = new();

        

        public delegate void InventoryContainerUpdated(AttachedContainer container);

        public event InventoryContainerUpdated OnInventoryContainerAdded;

        public event InventoryContainerUpdated OnInventoryContainerRemoved;

        public InventoryView InventoryView { get; private set; }

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
        /// <param name="index">The position of the container for a given type, if there's two pocket containers, it'd be 0 and 1</param>
        /// <param name="IDContainer"></param>
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
                else if(container.Type == ContainerType.Identification)
                {
                    typeIndex++;
                }
            }
            typeContainer = null;
            return false;
        }

        public int CountHands => OnPlayerContainers.Where(x => x.Type == ContainerType.Hand).Count();

        protected override void OnAwake()
        {
            base.OnAwake();

            Hands.Inventory = this;

        }



        protected override void OnStart()
        {
            base.OnStart();
            // Start by adding all containers on the human in the inventory
            SetupView();
            var attachedContainers = GetComponentsInChildren<AttachedContainer>();
            foreach (var container in attachedContainers)
            {
                AddContainer(container);
            }
            Subsystems.Get<RoleSystem>().GiveRoleLoadoutToPlayer(Body);
        }

        private void SetupView()
        {
            InventoryView = ViewLocator.Get<InventoryView>().First();
            InventoryView.Setup(this);
        }

        public void AddContainer(AttachedContainer container)
        {
            OnPlayerContainers.Add(container);
            OnInventoryContainerAdded?.Invoke(container);
        }

        public void RemoveContainer(AttachedContainer container)
        {
            OnPlayerContainers.Remove(container);
            OnInventoryContainerRemoved?.Invoke(container);
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
    }
}
