﻿using System;
using System.Collections.Generic;
using Coimbra;
using SS3D.Systems.Storage.Containers;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Systems.Storage.UI
{
    /// <summary>
    /// Coordinates the players inventory ui
    /// </summary>
    public class InventoryUi : MonoBehaviour
    {
        [NonSerialized]
        public Inventory Inventory;
        public HandsUi HandsUi;
        /// <summary>
        /// The prefab for a container display
        /// </summary>
        public GameObject ContainerUiPrefab;

        private readonly List<ContainerDisplay> _containerDisplays = new();
        
        public void Start()
        {
            Assert.IsNotNull(HandsUi);
            Assert.IsNotNull(Inventory);

            HandsUi.Hands = Inventory.Hands;
            
            Inventory.ContainerOpened += InventoryOnContainerOpened;
            Inventory.ContainerClosed += InventoryOnContainerClosed;
        }

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