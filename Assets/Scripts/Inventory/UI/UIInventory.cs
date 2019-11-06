using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/**
 * Connects a player's inventory with the UI.
 * 
 * The UIInventory collects containers the player has access to them, and
 * hands them out to UI Elements.
 * 
 * Containers attached to the player are dealt with two 'special' ui elements, one for the body, one for the hotbar.
 * All other containers are handled by a creating a 'generic' container.
 */
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
    // The existing ui element for the player body
    public ContainerRenderer playerBodyView;
    // The existing ui element for the hotbar area
    public ContainerRenderer hotbarView;

    public void StartUI(Inventory inventory)
    {
        this.inventory = inventory;
        inventory.EventOnChange += (a, b, c) => OnInventoryChange();
        OnInventoryChange();
    }

    private void OnInventoryChange()
    {
        var containers = inventory.GetContainers();
        // Collect each container by owner
        Dictionary<GameObject, List<Container>> containerSets = containers.GroupBy(container => container.gameObject).ToDictionary(group => group.Key, group => group.ToList());
        
        foreach (var containerSet in containerSets)
        {
            // If this is the player, set the specific player views
            if(containerSet.Value.Count > 0 && containerSet.Value[0].isLocalPlayer)
            {
                playerBodyView.UpdateContainers(containerSet.Key, containerSet.Value);
                hotbarView.UpdateContainers(containerSet.Key, containerSet.Value);
                continue;
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

    private List<ContainerRenderer> handlers = new List<ContainerRenderer>();
    private Inventory inventory;
}