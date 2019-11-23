using Mirror;
using System.Collections.Generic;
using UnityEngine;

/**
 * This is the basic inventory system. Any inventory-capable creature should have this component.
 * The basic inventory system has to handle:
 *  - Aggregating all containers on the player and accessible to the player
 *  - The moving of items from one item-slot to another
 */
public class Inventory : NetworkBehaviour
{
    public struct SlotReference
    {
        public SlotReference(Container container, int slotIndex)
        {
            this.container = container;
            this.slotIndex = slotIndex;
        }

        public Container container;
        public int slotIndex;
    }

    private class GameObjectList : SyncList<GameObject> { }

    // Called whenever the containers change
    public event GameObjectList.SyncListChanged EventOnChange {
        add { objectSources.Callback += value; }
        remove { objectSources.Callback -= value; }
    }
    
    // The slot the player currently has selected. May be null (container will be null, slotindex will be -1)
    // Note: NOT SYNCHRONIZED. LOCAL PLAYER ONLY
    public SlotReference holdingSlot = new SlotReference(null, -1);

    /**
     * Add an item from the world into a container.
     */
    [Command]
    public void CmdAddItem(GameObject item, GameObject toContainer, int toIndex)
    {
        Despawn(item);
        toContainer.GetComponent<Container>().AddItem(toIndex, item);
    }

    /**
     * Place an item from a container into the world.
     */
    [Command]
    public void CmdPlaceItem(GameObject fromContainer, int fromIndex, Vector3 location, Quaternion rotation)
    {
        GameObject item = fromContainer.GetComponent<Container>().RemoveItem(fromIndex);
        Spawn(item, location, rotation);
    }

    /**
     * Move an item from one container to another.
     * This is intended to be called by the UI, when the user drags an item from one place to another
     */
    [Command]
    public void CmdMoveItem(GameObject fromContainer, int fromIndex, GameObject toContainer, int toIndex)
    {
        var from = fromContainer.GetComponent<Container>();
        var to = toContainer.GetComponent<Container>();

        if (!Container.AreCompatible(to.GetSlot(toIndex), from.GetItem(fromIndex).itemType))
            throw new System.Exception("Item not compatible with slot");

        GameObject item = from.RemoveItem(fromIndex);
        to.AddItem(toIndex, item);
    }

    public List<Container> GetContainers()
    {
        List<Container> containers = new List<Container>();

        foreach (var obj in objectSources)
        {
            if (obj == null)
            {
                Debug.Log("Still have that mirror bug where transmitting self in OnStartServer for some reason doesnt fucking work");
            }
            else
            {
                containers.AddRange(obj.GetComponents<Container>());
                containers.AddRange(obj.GetComponentsInChildren<Container>());
            }
        }

        return containers;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        CmdAddSelf();
    }

    /**
     * Sets up the containers. Must run on server.
     * Only called in OnStartLocalPlayer. If I try to run the AddSelf code directly
     * in OnStartServer the thing has a fucking tantrum and just adds null to the objectSources list.
     */
    [Command]
    private void CmdAddSelf()
    {
        objectSources.Add(gameObject);
    }

    /**
     * Graphically removes the object from the world (for server and all clients).
     * Must be called from server initially
     */
    private void Despawn(GameObject item)
    {
        // If on server we can do things to the transform
        item.transform.SetPositionAndRotation(new Vector3(), new Quaternion());
        item.SetActive(false);

        if (isServer)
            RpcDespawn(item);
    }
    [ClientRpc]
    private void RpcDespawn(GameObject item)
    {
        if (!isServer) // Prevent server double-dipping
            Despawn(item);
    }

    /**
     * Graphically adds the item back into the world (for server and all clients).
     * Must be called from server initially
     */
    private void Spawn(GameObject item, Vector3 position, Quaternion rotation = new Quaternion())
    {
        item.transform.SetPositionAndRotation(position, rotation);
        item.SetActive(true);

        if (isServer)
            RpcSpawn(item, position, rotation);
    }
    [ClientRpc]
    private void RpcSpawn(GameObject item, Vector3 position, Quaternion rotation)
    {
        if (!isServer) // Silly thing to prevent looping when server and client are one
            Spawn(item, position, rotation);
    }

    // All objects containing containers usable by this player
    private readonly GameObjectList objectSources = new GameObjectList();
}