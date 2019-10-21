using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/**
 * This is the basic inventory system. Any inventory-capable creature should have this component.
 * The basic inventory system has to handle:
 *  - Aggregating all containers on the player
 *  - The moving of items from one item-slot to another
 */
public class Inventory : NetworkBehaviour
{
    /**
     * Move an item from one container to another.
     * This is intended to be called by the UI, when the user drags an item from one place to another
     */
    [Command]
    public void CmdMoveItem(Container from, int fromIndex, Container to, int toIndex)
    {
        // TODO: Check for compatibility and etc.
        Item item = from.items[fromIndex];
        from.RemoveItem(fromIndex);
        to.AddItem(toIndex, item);
    }

    private void Start()
    {
        // Search through and add all containers.
        var ownedContainers = GetComponents<Container>();
        containers.AddRange(ownedContainers);

        // Connect the UI
        if (isLocalPlayer)
            GameObject.Find("Inventory UI").GetComponent<InventoryUI>().SetInventory(this);
    }



    /*    private void Update()
        {
            if (!isLocalPlayer) return;

            if (Input.GetButtonDown("DropActive"))
            {
                RemoveItem(GetActiveHandSlot());
            }

            if (Input.GetButtonDown("Click"))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, 100))
                {
                    Item item = hit.transform.gameObject.GetComponent<Item>();
                    if (item != null) AddItem(item);
                }
            }
        }*/

    // The containers for this player's inventory
    public List<Container> containers;
}