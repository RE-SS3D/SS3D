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

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// InventoryUi handle displaying the containers UI showing up when opening a container.
    /// It handles displaying all the containers opened by the player, and removing the display when a container is closed.
    /// It includes containers such as the one in the world the player interact with (toolbox, lockers) as well as the one
    /// </summary>
    public class InventoryView : View
    {
        public Containers.Inventory Inventory;

        public HandsView HandsView;
        public InventorySlotView PocketView;

        public GameObject PocketPrefab;
        public Transform PocketParent;
        /// <summary>
        /// The prefab for a container display
        /// </summary>
        public GameObject ContainerUiPrefab;

        [SerializeField] private GameObject _uiPanel;

        private readonly List<ContainerDisplay> _containerDisplays = new();

        // Maybe HandsUI should only handle selected hand highlight and inventory UI
        // should handle setting up containers to UI.
        public void Setup()
        {
            Assert.IsNotNull(HandsView);
            Assert.IsNotNull(Inventory);

            HandsView.Hands = Inventory.Hands;

            Inventory.OnContainerOpened += InventoryOnContainerOpened;
            Inventory.OnContainerClosed += InventoryOnContainerClosed;

            InventorySlotView pocketView = ViewLocator.Get<InventorySlotView>().First();
            pocketView.Inventory = Inventory;
            pocketView.Setup();
        }

        /// <summary>
        /// Sets the UI enabled.
        /// </summary>
        /// <param name="enabled"></param>
        public void Enable(bool enabled)
        {
            _uiPanel.SetActive(enabled);
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
            //containerUi.Inventory = Inventory;
            _containerDisplays.Add(new ContainerDisplay(ui, container));
        }
    }
}