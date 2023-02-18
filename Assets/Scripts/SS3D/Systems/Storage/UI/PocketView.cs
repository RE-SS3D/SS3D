using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Core.Behaviours;
using SS3D.Systems.Storage.Containers;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Storage.UI
{
    /// <summary>
    /// Handle setting up the UI of pocket containers.
    /// </summary>
    /// <summary>
    /// Handle setting up the UI of pocket containers.
    /// </summary>
    public class PocketView : View
    {
        [NonSerialized]
        public Inventory Inventory;
        public GameObject PocketPrefab;

        public Transform TransformParent;

        [FormerlySerializedAs("DummyLeftPocketViewGameObject")] public GameObject LeftPocketGameObject;
        [FormerlySerializedAs("DummyRightPocketViewGameObject")] public GameObject RightPocketGameObject;

        public void Setup()
        {
            // Remove dummy slots in the prefab HumanoidInventory
            DestroyImmediate(RightPocketGameObject);
            DestroyImmediate(LeftPocketGameObject);

            // Find all containers with type Pocket on the Human prefab.
            ContainerDescriptor[] inventoryContainers = Inventory.GetComponentsInChildren<ContainerDescriptor>(true);
            List<ContainerDescriptor> pocketContainers = inventoryContainers.Where(container => container.Type is ContainerType.Pocket).ToList();

            if (pocketContainers.Count == 0)
            {
                throw new ApplicationException("no container of type pocket is present on " + "the inventory's game object or any of it's children.");
            }

            // Set up the container that each pocket display.
            foreach (ContainerDescriptor container in pocketContainers)
            {
                AttachedContainer attachedContainer = container.AttachedContainer;
                GameObject handElement = Instantiate(PocketPrefab, TransformParent, false);

                SingleItemContainerSlot slot = handElement.GetComponent<SingleItemContainerSlot>();
                slot.Inventory = Inventory;
                slot.Container = attachedContainer;
            }
        }
    }
}
