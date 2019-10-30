using UnityEngine;
using System.Collections;
using Mirror;

public class HandContainer : Container
{
    public GameObject[] handSlots;

    // Override add item so any changes refresh the interaction system's tool
    public override void AddItem(int index, GameObject item)
    {
        base.AddItem(index, item);

        // Place item in hand
        PlaceItem(index, item);
    }
    public override GameObject RemoveItem(int slot)
    {
        UnplaceItem(slot, GetItem(slot).gameObject);

        return base.RemoveItem(slot);
    }

    // Set the slot requirements defaults
    private void Reset()
    {
        // Set defaults for container.
        containerName = "Hands";
        containerType = Type.Interactors;
        slots = new SlotType[2];
        slots[0] = SlotType.LeftHand;
        slots[1] = SlotType.RightHand;
    }
    public override void OnStartClient()
    {
        for (int i = 0; i < Length(); ++i)
        {
            var item = GetItem(i);
            if (item)
                PlaceItem(i, item.gameObject);
        }
    }

    private void PlaceItem(int index, GameObject item)
    {
        item.SetActive(true);

        // Determine physics status
        item.GetComponent<Rigidbody>().isKinematic = true;
        item.GetComponent<BoxCollider>().enabled = false;
        if (item.GetComponent<NetworkTransform>())
            item.GetComponent<NetworkTransform>().enabled = false;

        item.transform.SetParent(handSlots[index].transform, false);
        item.transform.localPosition = new Vector3();
        item.transform.localRotation = new Quaternion();

        if (isServer)
            RpcPlaceItem(index, item);
    }
    private void UnplaceItem(int index, GameObject item)
    {
        item.SetActive(false);

        // Determine physics status
        item.GetComponent<Rigidbody>().isKinematic = false;
        item.GetComponent<BoxCollider>().enabled = true;
        if (item.GetComponent<NetworkTransform>())
            item.GetComponent<NetworkTransform>().enabled = true;

        item.transform.SetParent(null);

        if (isServer)
            RpcUnplaceItem(index, item);
    }

    [ClientRpc]
    private void RpcPlaceItem(int index, GameObject item)
    {
        if (!isServer)
            PlaceItem(index, item);
    }
    [ClientRpc]
    private void RpcUnplaceItem(int index, GameObject item)
    {
        if (!isServer)
            UnplaceItem(index, item);
    }

}
