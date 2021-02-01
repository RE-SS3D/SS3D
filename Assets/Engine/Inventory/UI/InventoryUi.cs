using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Engine.Inventory.UI
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

        private List<ContainerDisplay> containerDisplays = new List<ContainerDisplay>();
        
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
            for (var i = 0; i < containerDisplays.Count; i++)
            {
                if (containerDisplays[i].Container == container)
                {
                    Destroy(containerDisplays[i].UiElement);
                    containerDisplays.RemoveAt(i);
                    return;
                }
            }
        }

        private void InventoryOnContainerOpened(AttachedContainer container)
        {
            foreach (ContainerDisplay x in containerDisplays)
            {
                if (x.Container == container)
                {
                    return;
                }
            }

            GameObject ui = Instantiate(ContainerUiPrefab);
            var containerUi = ui.GetComponent<ContainerUi>();
            Assert.IsNotNull(containerUi);
            containerUi.AttachedContainer = container;
            containerUi.Inventory = Inventory;
            containerDisplays.Add(new ContainerDisplay(ui, container));
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