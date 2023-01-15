﻿using System;
using System.Collections.Generic;
using Coimbra;
using SS3D.Core.Behaviours;
using SS3D.Systems.Storage.Containers;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace SS3D.Systems.Storage.UI
{
    /// <summary>
    /// InventoryUi handle displaying the containers UI showing up when opening a container.
    /// It handles displaying all the containers opened by the player, and removing the display when a container is closed.
    /// It includes containers such as the one in the world the player interact with (toolbox, lockers) as well as the one
    /// </summary>
    public class InventoryView : View
    {
        [NonSerialized]
        public Inventory Inventory;
        public HandsUi HandsUi;
        public GameObject PocketPrefab;
        public Transform PocketParent;
        /// <summary>
        /// The prefab for a container display
        /// </summary>
        public GameObject ContainerUiPrefab;

        private readonly List<ContainerDisplay> _containerDisplays = new();
        
        // Maybe HandsUI should only handle selected hand highlight and inventory UI
        // should handle setting up containers to UI.
        public void Start()
        {
            Assert.IsNotNull(HandsView);
            Assert.IsNotNull(Inventory);

            HandsUi.Hands = Inventory.Hands;

            Inventory.ContainerOpened += InventoryOnContainerOpened;
            Inventory.ContainerClosed += InventoryOnContainerClosed;
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

                _containerDisplays[i].UiElement.Destroy();
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
            containerUi.Inventory = Inventory;
            _containerDisplays.Add(new ContainerDisplay(ui, container));
        }

        private struct ContainerDisplay
        {
            public GameObject UiElement;
            public AttachedContainer Container;

            public ContainerDisplay(GameObject uiElement, AttachedContainer container)
            {
                UiElement = uiElement;
                Container = container;
            }
        }
    }
}