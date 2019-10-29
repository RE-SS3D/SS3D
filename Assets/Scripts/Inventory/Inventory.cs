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
    public delegate void OnChange();
    private class GameObjectList : SyncList<GameObject> { }

    // Called whenever the containers change
    [SyncEvent]
    public event OnChange EventOnChange;

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
    public void CmdPlaceItem(Vector3 location, GameObject fromContainer, int fromIndex)
    {
        GameObject item = fromContainer.GetComponent<Container>().RemoveItem(fromIndex);
        Spawn(item, location);
    }

    /**
     * Move an item from one container to another.
     * This is intended to be called by the UI, when the user drags an item from one place to another
     */
    [Command]
    public void CmdMoveItem(GameObject fromContainer, int fromIndex, GameObject toContainer, int toIndex)
    {
        // TODO: Check for compatibility and etc.
        GameObject item = fromContainer.GetComponent<Container>().RemoveItem(fromIndex);
        toContainer.GetComponent<Container>().AddItem(toIndex, item);
    }

    public List<Container> GetContainers()
    {
        List<Container> containers = new List<Container>();

        foreach (var gameObject in objectSources)
            containers.AddRange(gameObject.GetComponents<Container>());

        return containers;
    }

    /**
     * Sets up the containers. Must run on server.
     */
    private void Start()
    {
        if (!isServer)
        {
            if (isLocalPlayer)
                CmdStart();
            return;
        }

        // Search through and add all containers.
        objectSources.Add(gameObject);
    }
    [Command]
    private void CmdStart()
    {
        Start();
    }


    /**
     * Performs the necessary item stuff to actually pick up the object
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
        if(!isServer) // Prevent server double-dipping
            Despawn(item);
    }

    /**
     * Performs the necessary item stuff to drop the object
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
        if(!isServer) // Silly thing to prevent looping when server and client are one
            Spawn(item, position, rotation);
    }

    // All containers accessible to the player
    // TODO: Sync List probably doesn't need to be used
    private readonly GameObjectList objectSources = new GameObjectList();
}