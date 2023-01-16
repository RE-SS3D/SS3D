using SS3D.Systems.Storage.Containers;
using SS3D.Systems.Storage.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlasticGui.LaunchDiffParameters;


/// <summary>
/// Handle setting up the UI of pocket containers.
/// </summary>
public class PocketView : MonoBehaviour
{
    [NonSerialized]
    public Inventory Inventory;
    public GameObject PocketPrefab;
    public GameObject DummyLeftPocketViewGameObject;
    public GameObject DummyRightPocketViewGameObject;
    // Start is called before the first frame update
    void Start()
    {
        SetUpPocketUI();
    }

    private void SetUpPocketUI()
    {
        // Remove dummy slots in the prefab HumanoidInventory
        DestroyImmediate(DummyRightPocketViewGameObject);
        DestroyImmediate(DummyLeftPocketViewGameObject);

        // Find all containers with type Pocket on the Human prefab.
        var InventoryContainers = Inventory.gameObject.GetComponentsInChildren<ContainerDescriptor>();
        var PocketContainers = new List<ContainerDescriptor>();
        foreach (var container in InventoryContainers)
        {
            if (container.ContainerType is ContainerType.Pocket)
                PocketContainers.Add(container);
        }
        if (PocketContainers.Count == 0)
        {
            throw new ApplicationException("no container of type pocket is present on " +
                "the inventory's game object or any of it's children.");
        }

        // Set up the container that each pocket display.
        foreach (var container in PocketContainers)
        {
            var attachedContainer = container.AttachedContainer;
            GameObject handElement = Instantiate(PocketPrefab, transform, false);
            SingleItemContainerSlot slot = handElement.GetComponent<SingleItemContainerSlot>();
            slot.Inventory = Inventory;
            slot.Container = attachedContainer;
        }
    }
}
