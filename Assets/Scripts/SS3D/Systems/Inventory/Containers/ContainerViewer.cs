using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static UnityEngine.UI.GridLayoutGroup;

namespace SS3D.Systems.Inventory.Containers
{
    public class ContainerViewer : NetworkActor
    {
        public delegate void ContainerEventHandler(AttachedContainer container);

        public event ContainerEventHandler OnContainerOpened;

        public event ContainerEventHandler OnContainerClosed;

        private float _nextAccessCheck;

        private readonly List<AttachedContainer> _openedContainers = new();

        public Inventory inventory;

        public ContainerView containerView;

        protected override void OnAwake()
        {
            base.OnAwake();
            SetupView();
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        private void SetupView()
        {
            containerView = ViewLocator.Get<ContainerView>().First();
            containerView.Setup(this);
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
                if (HasContainer(container) && observer != hands)
                {
                    return;
                }
            }

            container.ContainerInteractive.SetOpenState(state);
        }

        /// <summary>
        /// Does this inventory have a specific container ?
        /// </summary>
        public bool HasContainer(AttachedContainer container)
        {
            return _openedContainers.Contains(container);
        }

        public bool CanModifyContainer(AttachedContainer container)
        {
            // TODO: This root transform check might allow you to take out your own organs down the road O_O
            return _openedContainers.Contains(container) || container.transform.root == transform;
        }

        [TargetRpc]
        private void TargetOpenContainer(NetworkConnection target, AttachedContainer container)
        {
            InvokeContainerOpened(container);
        }

        private void InvokeContainerOpened(AttachedContainer container)
        {
            OnContainerOpened?.Invoke(container);
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
        public void CloseContainer(AttachedContainer container)
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

                CloseContainer(attachedContainer);
                i--;
            }

            _nextAccessCheck = time + 0.5f;
        }

        [ServerRpc]
        public void CmdContainerClose(AttachedContainer container)
        {
            CloseContainer(container);
        }


        [TargetRpc]
        private void TargetCloseContainer(NetworkConnection target, AttachedContainer container)
        {
            InvokeContainerClosed(container);
        }

        private void InvokeContainerClosed(AttachedContainer container)
        {
            OnContainerClosed?.Invoke(container);
        }

    }
}
