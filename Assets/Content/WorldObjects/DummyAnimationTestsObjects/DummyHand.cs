using System;
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
    
    public Transform ChooseTargetLocker(TargetLockerType type)
    {
        Transform targetToSet;
        
        switch (type)
        {
            case TargetLockerType.Pickup:
                targetToSet = pickupTargetLocker;
                break;
            case TargetLockerType.Hold:
                targetToSet = handHoldTargetLocker;
                break;
            case TargetLockerType.ItemPosition:
                targetToSet = itemPositionTargetLocker;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        return targetToSet;
    }

    public void SetWorldPositionRotationOfIkTarget(TargetLockerType type, Transform toCopy)
    {
        Transform targetToSet = ChooseTargetLocker(type);
        
        targetToSet.position = toCopy.position;
        targetToSet.rotation = toCopy.rotation;
    }

    public void SetParentTransformOfIkTarget(TargetLockerType type, Transform parent)
    {
        Transform targetToSet = ChooseTargetLocker(type);
        
        targetToSet.parent = parent;
        targetToSet.localPosition = Vector3.zero;
        targetToSet.localRotation = Quaternion.identity;
    }
    
}
