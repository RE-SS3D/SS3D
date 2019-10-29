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
    // TODO: If any item is in hands on start, should be done in Start()
    protected override void Start()
    {
        base.Start();

        for (int i = 0; i < handSlots.Length; ++i)
        {
            var item = GetItem(i);
            if(item)
                PlaceItem(i, item.gameObject);
        }
    }

    private void PlaceItem(int index, GameObject item)
    {
        item.transform.SetPositionAndRotation(new Vector3(), new Quaternion());
        item.SetActive(true);

        // Determine physics status
        item.GetComponent<Rigidbody>().isKinematic = true;
        item.GetComponent<BoxCollider>().enabled = false;

        item.transform.SetParent(handSlots[index].transform, false);

        if (isServer)
            RpcPlaceItem(index, item);
    }
    private void UnplaceItem(int index, GameObject item)
    {
        item.SetActive(false);

        // Determine physics status
        item.GetComponent<Rigidbody>().isKinematic = false;
        item.GetComponent<BoxCollider>().enabled = true;

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
