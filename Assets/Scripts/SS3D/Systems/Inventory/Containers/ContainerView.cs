using Coimbra;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using NetworkView = SS3D.Core.Behaviours.NetworkView;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// Add and remove UIs for containers.
    /// </summary>
    public class ContainerView : NetworkView
    {
        /// <summary>
        /// List of displayed containers on the player screen.
        /// </summary>
        private readonly List<ContainerDisplay> _containerDisplays = new();

        /// <summary>
        /// The prefab for a container display
        /// </summary>
        [FormerlySerializedAs("ContainerUiPrefab")]
        [SerializeField]
        private GameObject _containerUiPrefab;

        /// <summary>
        /// Does the player have the UI opened for a specific container ?
        /// </summary>
        public bool ContainerIsDisplayed(AttachedContainer container)
        {
            return _containerDisplays.Select(x => x.Container).Contains(container);
        }

        /// <summary>
        /// If the container is not already showing up, instantiate a container UI to display the container.
        /// </summary>
        [TargetRpc]
        public void RpcOpenContainer(NetworkConnection target, AttachedContainer container, NetworkObject inventoryGameObject)
        {
            foreach (ContainerDisplay x in _containerDisplays)
            {
                if (x.Container == container)
                {
                    return;
                }
            }

            GameObject ui = Instantiate(_containerUiPrefab);
            ContainerUiDisplay containerUiDisplay = ui.GetComponent<ContainerUiDisplay>();
            containerUiDisplay.Init(container, inventoryGameObject.GetComponent<IInventory>());
            _containerDisplays.Add(new ContainerDisplay(ui, container));
        }

        /// <summary>
        /// Remove any instance of UI showing up the inside of the container passed in argument.
        /// </summary>
        [TargetRpc]
        public void RpcCloseContainer(NetworkConnection target, AttachedContainer container)
        {
            for (int i = 0; i < _containerDisplays.Count; i++)
            {
                if (_containerDisplays[i].Container != container)
                {
                    continue;
                }

                _containerDisplays[i].UiElement.Dispose(true);
                _containerDisplays.RemoveAt(i);
                return;
            }
        }
    }
}
