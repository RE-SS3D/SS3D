using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public Inventory inventory;
    public GameObject containerPrefab;

    public void SetInventory(Inventory inventory)
    {
        this.inventory = inventory;

        foreach(Container container in inventory.containers)
        {
            GameObject containerObject = Instantiate(containerPrefab, new Vector3(0f, 400f, 0f), new Quaternion());
            containerObject.GetComponent<ContainerUI>().container = container;
            containerObject.transform.SetParent(transform, false);
        }
    }
}