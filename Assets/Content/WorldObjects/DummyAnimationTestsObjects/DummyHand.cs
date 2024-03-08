using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyHand : MonoBehaviour
{
    public DummyItem itemInHand;

    public DummyHands.HandType handType;

    public Transform handHoldTargetLocker;

    public Transform pickupTargetLocker;

    public Transform itemPositionTargetLocker;

    public Transform upperArm;

    public bool Full => itemInHand != null;

    public bool Empty => itemInHand == null;

    public void RemoveItem()
    {
        itemInHand.transform.parent = null;
        itemInHand.gameObject.GetComponent<Rigidbody>().isKinematic = false;
        itemInHand.gameObject.GetComponent<Collider>().enabled = true;
        itemInHand = null;
    }

    public void AddItem(DummyItem item)
    {
        itemInHand = item;
        item.GetComponent<Rigidbody>().isKinematic = true;
        item.GetComponent<Collider>().enabled = false;
    }
    
}
