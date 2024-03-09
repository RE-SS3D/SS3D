using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyHand : MonoBehaviour
{
    public DummyItem item;

    public HandType handType;

    public Transform handHoldTargetLocker;

    public Transform pickupTargetLocker;

    public Transform itemPositionTargetLocker;

    public Transform upperArm;

    public bool Full => item != null;

    public bool Empty => item == null;

    public void RemoveItem()
    {
        item.transform.parent = null;
        item.gameObject.GetComponent<Rigidbody>().isKinematic = false;
        item.gameObject.GetComponent<Collider>().enabled = true;
        item = null;
    }

    public void AddItem(DummyItem itemAdded)
    {
        item = itemAdded;
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

    public void SetWorldPositionRotationTargetLocker(TargetLockerType type, Transform toCopy)
    {
        Transform targetToSet = ChooseTargetLocker(type);
        
        targetToSet.position = toCopy.position;
        targetToSet.rotation = toCopy.rotation;
    }

    public void SetParentTransformTargetLocker(TargetLockerType type, Transform parent)
    {
        Transform targetToSet = ChooseTargetLocker(type);
        
        targetToSet.parent = parent;
        targetToSet.localPosition = Vector3.zero;
        targetToSet.localRotation = Quaternion.identity;
    }
    
}
