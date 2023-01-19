using SS3D.Systems.Storage.Containers;
using SS3D.Systems.Storage.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;



/// <summary>
/// Handle setting up the UI of pocket containers.
/// </summary>
public class InventorySlotView : MonoBehaviour
{
    [NonSerialized]
    public Inventory Inventory;

    public GameObject IDSlotPrefab;
    public GameObject PocketPrefab;

    public GameObject DummyIdSlotViewGameObject;
    public GameObject DummyLeftPocketViewGameObject;
    public GameObject DummyRightPocketViewGameObject;
    // Start is called before the first frame update
    void Start()
    {
        SetUpUI();
    }

    private void SetUpUI()
    {
        // Remove dummy slots in the prefab HumanoidInventory
        DestroyImmediate(DummyIdSlotViewGameObject);
        DestroyImmediate(DummyRightPocketViewGameObject);
        DestroyImmediate(DummyLeftPocketViewGameObject);

        // Find all containers with type Pocket on the Human prefab.
        var _inventoryContainers = Inventory.gameObject.GetComponentsInChildren<ContainerDescriptor>();
        var _pocketContainers = new List<ContainerDescriptor>();
        var _idContainer = new ContainerDescriptor();

        foreach (var container in _inventoryContainers)
        {
            if (container.ContainerType is ContainerType.Pocket)
                _pocketContainers.Add(container);

            if (container.ContainerType is ContainerType.ID)
                _idContainer = container;

        }

        if (_pocketContainers.Count == 0)
        {
            throw new ApplicationException("no container of type pocket is present on " +
                "the inventory's game object or any of it's children.");
        }

        if (_idContainer == null)
            throw new ApplicationException("no container of type id slot is present on " +
                "the inventory's game object or any of it's children.");

        // Set up the container for the ID slot display.
        SetUpSlot(_idContainer, IDSlotPrefab);
        Inventory.IDContainer = _idContainer.Container;

        // Set up the container that each pocket display.
        foreach (var container in _pocketContainers)
        {
            SetUpSlot(container, PocketPrefab);
        }
    }

    private void SetUpSlot(ContainerDescriptor _container, GameObject _prefab)
    {
        var attachedContainer = _container.AttachedContainer;
        GameObject handElement = Instantiate(_prefab, transform, false);
        SingleItemContainerSlot slot = handElement.GetComponent<SingleItemContainerSlot>();
        slot.Inventory = Inventory;
        slot.Container = attachedContainer;
    }
}
