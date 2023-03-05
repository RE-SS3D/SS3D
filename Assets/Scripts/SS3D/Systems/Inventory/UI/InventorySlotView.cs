using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// Handle setting up the UI of pocket containers.
    /// </summary>
    public class InventorySlotView : View
    {
        [NonSerialized]
        public Containers.Inventory Inventory;
        public GameObject PocketPrefab;
        public GameObject IDSlotPrefab;

        public Transform TransformParent;

        [FormerlySerializedAs("DummyIDSlotViewGameObject")] public GameObject IDSlotGameObject;
        [FormerlySerializedAs("DummyLeftPocketViewGameObject")] public GameObject LeftPocketGameObject;
        [FormerlySerializedAs("DummyRightPocketViewGameObject")] public GameObject RightPocketGameObject;

        public void Setup()
        {
            // Remove dummy slots in the prefab HumanoidInventory
            DestroyImmediate(IDSlotGameObject);
            DestroyImmediate(LeftPocketGameObject);
            DestroyImmediate(RightPocketGameObject);

            // Find all containers with type Pocket on the Human prefab.
            ContainerDescriptor[] inventoryContainers = Inventory.GetComponentsInChildren<ContainerDescriptor>(true);
            
            List<ContainerDescriptor> pocketContainers = inventoryContainers.Where(container => container.Type is ContainerType.Pocket).ToList();
            ContainerDescriptor idSlotContainer = inventoryContainers.Where(container => container.Type is ContainerType.Identification).FirstOrDefault();

            if (pocketContainers.Count == 0)
            {
                throw new ApplicationException("no container of type pocket in " +
                    "the inventory's game object or any of it's children.");
            }

            if (idSlotContainer == null)
            {
                throw new ApplicationException("no container of type identification in " +
                    "the inventory's game object or any of it's children.");
            }

            // Set up the container for the id slot display and make it the first slot.
            var idSlot = SetUpSlot(idSlotContainer, IDSlotPrefab);
            idSlot.transform.SetSiblingIndex(0);

            // Set up the container that each pocket display.
            foreach (ContainerDescriptor container in pocketContainers)
            {
                SetUpSlot(container, PocketPrefab);
            }
        }

        private SingleItemContainerSlot SetUpSlot(ContainerDescriptor container, GameObject prefab)
        {
            AttachedContainer attachedContainer = container.AttachedContainer;
            GameObject handElement = Instantiate(prefab, TransformParent, false);

            SingleItemContainerSlot slot = handElement.GetComponent<SingleItemContainerSlot>();
            slot.Inventory = Inventory;
            slot.Container = attachedContainer;

            return slot;
        }
    }
}
