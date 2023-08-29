using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using SS3D.Logging;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// Add and remove UIs for containers.
    /// </summary>
    public class ContainerView : View
    {
        /// <summary>
        /// The script handling logic regarding when to remove and add container UIs.
        /// </summary>
        private ContainerViewer containerViewer;

        /// <summary>
        /// List of displayed containers on the player screen.
        /// </summary>
        private readonly List<ContainerDisplay> _containerDisplays = new();

        /// <summary>
        /// The prefab for a container display
        /// </summary>
        public GameObject ContainerUiPrefab;

        public void Setup(ContainerViewer viewer)
        {
            containerViewer = viewer;
            viewer.OnContainerOpened += InventoryOnContainerOpened;
            viewer.OnContainerClosed += InventoryOnContainerClosed;
        }

        /// <summary>
        /// Remove any instance of UI showing up the inside of the container passed in argument.
        /// </summary>
        private void InventoryOnContainerClosed(AttachedContainer container)
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

        /// <summary>
        /// If the container is not already showing up, instantiate a container UI to display the container.
        /// </summary>
        private void InventoryOnContainerOpened(AttachedContainer container)
        {
            foreach (ContainerDisplay x in _containerDisplays)
            {
                if (x.Container == container)
                {
                    return;
                }
            }

            GameObject ui = Instantiate(ContainerUiPrefab);
            ContainerUi containerUi = ui.GetComponent<ContainerUi>();
            Assert.IsNotNull(containerUi);
            containerUi.AttachedContainer = container;
            containerUi.Inventory = containerViewer.inventory;
            _containerDisplays.Add(new ContainerDisplay(ui, container));
        }
    }
}

