using System;
using System.Collections.Generic;
using Coimbra;
using SS3D.Systems.Storage.Containers;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Systems.Storage.UI
{
    /// <summary>
    /// InventoryUi handle displaying the containers UI showing up when opening a container.
    /// It handles displaying all the containers opened by the player, and removing the display when a container is closed.
    /// It includes containers such as the one in the world the player interact with (toolbox, lockers) as well as the one
    /// </summary>
    public class InventoryUi : MonoBehaviour
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
            Assert.IsNotNull(HandsUi);
            Assert.IsNotNull(Inventory);

            HandsUi.Hands = Inventory.Hands;
            SetUpPocketUI();

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

        private void SetUpPocketUI()
        {
            // Destroy existing elements and replace them by prefabs set up with their containers.
            var childrenToDestroy = new List<GameObject>();
            for (int i = 0; i < PocketParent.transform.childCount; i++)
            {
                if (PocketParent.transform.GetChild(i).gameObject.name.Contains("Pocket"))
                {
                    childrenToDestroy.Add(PocketParent.transform.GetChild(i).gameObject);
                }
            }

            foreach (var child in childrenToDestroy)
            {
                DestroyImmediate(child);
            }

            var InventoryContainers = Inventory.gameObject.GetComponentsInChildren<ContainerDescriptor>();
            var PocketContainers = new List<ContainerDescriptor>();
            foreach (var container in InventoryContainers)
            {
                if (container.ContainerName.Contains("pocket"))
                    PocketContainers.Add(container);
            }
            if (PocketContainers.Count == 0)
            {
                throw new ApplicationException("no container containing the word pocket is present on " +
                    "the inventory's game object or any of it's children.");
            }
            foreach (var container in PocketContainers)
            {
                var attachedContainer = container.AttachedContainer;
                GameObject handElement = Instantiate(PocketPrefab, PocketParent, false);
                SingleItemContainerSlot slot = handElement.GetComponent<SingleItemContainerSlot>();
                slot.Inventory = Inventory;
                slot.Container = attachedContainer;
            }
        }
    }
}