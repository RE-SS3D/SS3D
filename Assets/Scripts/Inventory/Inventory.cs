using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/**
 * This is the basic inventory system. Any inventory-capable creature should have this component.
 * The basic inventory system has to handle:
 *  - Aggregating all containers on the player and accessible to the player
 *  - The moving of items from one item-slot to another
 */
public class Inventory : NetworkBehaviour
{
    public delegate void OnChange(IReadOnlyList<Container> containers);

    // Called whenever the containers change
    public event OnChange onChange;

    /**
     * Add an item from the world into a container.
     */
    [Command]
    public void CmdAddItem(Item item, Container to, int toIndex)
    {
        item.Despawn();
        to.AddItem(toIndex, item);
    }

    /**
     * Place an item from a container into the world.
     */
    [Command]
    public void CmdPlaceItem(Vector3 location, Container from, int fromIndex)
    {
        Item item = from.RemoveItem(fromIndex);
        item.Spawn(location);
    }

    /**
     * Move an item from one container to another.
     * This is intended to be called by the UI, when the user drags an item from one place to another
     */
    [Command]
    public void CmdMoveItem(Container from, int fromIndex, Container to, int toIndex)
    {
        // TODO: Check for compatibility and etc.
        Item item = from.RemoveItem(fromIndex);
        to.AddItem(toIndex, item);
    }

    public IReadOnlyList<Container> GetContainers()
    {
        return containers.AsReadOnly();
    }
    public void AddContainer(Container container)
    {
        containers.Add(container);
        onChange(GetContainers());
    }
    public void RemoveContainer(Container container)
    {
        containers.Remove(container);
        onChange(GetContainers());
    }

    private void Start()
    {
        // Search through and add all containers.
        var ownedContainers = GetComponents<Container>();
        containers.AddRange(ownedContainers);

        // Connect the UI
        if (isLocalPlayer)
            GameObject.Find("Inventory UI").GetComponent<UIInventory>().SetInventory(this);
    }

    // All containers accessible to the player
    private List<Container> containers = new List<Container>();
}