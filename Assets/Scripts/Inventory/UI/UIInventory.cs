using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class UIInventory : MonoBehaviour
{
    public abstract class ContainerRenderer : MonoBehaviour
    {
        [NonSerialized]
        public GameObject owner;
        [NonSerialized]
        public List<Container> containers = new List<Container>();

        public abstract void UpdateContainers(GameObject owner, List<Container> containers);
    }

    // The prefab for when a new container needs to be made
    public GameObject genericContainerPrefab;

    public ContainerRenderer playerBodyView;
    public ContainerRenderer hotbarView;

    public void SetInventory(Inventory inventory)
    {
        if(this.inventory)
            this.inventory.onChange -= OnInventoryChange;

        this.inventory = inventory;

        inventory.onChange += OnInventoryChange;

        OnInventoryChange(inventory.GetContainers());
    }

    private void OnInventoryChange(IReadOnlyList<Container> containers)
    {
        // Collect each container by owner
        Dictionary<GameObject, List<Container>> containerSets = containers.GroupBy(container => container.owner).ToDictionary(group => group.Key, group => group.ToList());
        
        foreach (var containerSet in containerSets)
        {
            // If this is the player, set the specific player views
            if(containerSet.Value.Count > 0 && containerSet.Value[0].isLocalPlayer)
            {
                playerBodyView.UpdateContainers(containerSet.Key, containerSet.Value);
                hotbarView.UpdateContainers(containerSet.Key, containerSet.Value);
            }

            // If a handler for it exists
            var handler = handlers.Find(renderer => renderer.owner == containerSet.Key);
            if (handler != null)
            {
                if(!handler.containers.SequenceEqual(containerSet.Value))
                    handler.UpdateContainers(containerSet.Key, containerSet.Value);
            }
            else
            {
                // No handler exists so we create a new one.
                GameObject obj = Instantiate(genericContainerPrefab, new Vector3(100f, 100f), new Quaternion());
                handler = obj.GetComponent<ContainerRenderer>();
                handler.UpdateContainers(containerSet.Key, containerSet.Value);
                handlers.Add(handler);
            }
        }

        // Remove any no-longer-needed handlers
        var removeList = handlers.FindAll(handler => !containerSets.Keys.Contains(handler.owner));
        foreach (var handler in removeList)
            Destroy(handler);
        handlers = handlers.Except(removeList).ToList();
    }

    private List<ContainerRenderer> handlers;
    private Inventory inventory;
}