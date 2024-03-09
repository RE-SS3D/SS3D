using SS3D.Systems.Inventory.Containers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void Notify(bool removeItem);

public class DummyPickUp : MonoBehaviour
{

    public float itemMoveDuration;
    public float itemReachDuration;

    private float _pickUpDuration;

    public DummyIkController dummyIkController;

    public DummyHands hands;

    public event Notify OnHoldChange;


    private void Start()
    {
        _pickUpDuration = itemMoveDuration + itemReachDuration;
    }
    
    private void Update()
    {

        if (!Input.GetMouseButtonDown(0))
            return;
        
        if (GetComponent<DummyHands>().SelectedHand.Empty)
        {
            TryPickUp();
        }
        else
        {
            TryThrow();
        }

    }

    private void PickUp(DummyItem item)
    {
        GetComponent<DummyAnimatorController>().TriggerPickUp();

        StartCoroutine(StartPickUpCoroutines(item));
        
        GetComponent<DummyHands>().SelectedHand.AddItem(item);
        
        OnHoldChange?.Invoke(false);

    }

    private IEnumerator StartPickUpCoroutines(DummyItem item)
    {
        OrientTargetForHandRotation(hands.SelectedHand);
        StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(
            transform, item.transform, itemReachDuration, false, true));
        yield return ModifyPickUpIkRigWeightToReach();
        StartCoroutine(DummyTransformHelper.LerpTransform(item.transform,
            hands.SelectedHand.itemPositionTargetLocker, itemMoveDuration));
        yield return ModifyPickUpIkRigWeightToHold();
    }

    private void TryPickUp()
    {
        // Cast a ray from the mouse position into the scene
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Check if the ray hits any collider
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if the collider belongs to a GameObject
            GameObject obj = hit.collider.gameObject;

            // should add conditions to check other objects doesn't require two hands.
            // also check picked up object doesn't require two hands if other hand is full.
            if (obj.TryGetComponent(out DummyItem item))
            {
                PickUp(item);
            }
        }
    }

    private void TryThrow()
    {
        GetComponent<DummyAnimatorController>().TriggerThrow();
        // TODO put logic here in IK controller
        DummyItem item = hands.SelectedHand.itemInHand;

        item.heldWithOneHand = false;
        item.heldWithTwoHands = false;

        if (hands.SelectedHand.Full && hands.UnselectedHand.Empty && item.canHoldTwoHand)
        {
            dummyIkController.rightHandHoldTwoBoneIkConstraint.weight = 0;
            dummyIkController.leftHandHoldTwoBoneIkConstraint.weight = 0;
            
        }
        else if (hands.SelectedHand.Full  && item.canHoldOneHand)
        {
            if (hands.SelectedHand.handType == DummyHands.HandType.LeftHand)
            {
                dummyIkController.leftHandHoldTwoBoneIkConstraint.weight = 0;
            }
            else
            {
                dummyIkController.rightHandHoldTwoBoneIkConstraint.weight = 0;
            }
        }

        hands.SelectedHand.RemoveItem();

        OnHoldChange?.Invoke(true);
    }
    
    
    
    private IEnumerator ModifyPickUpIkRigWeightToReach()
    {
        float elapsedTime = 0f;

        while (elapsedTime < itemReachDuration)
        {
            dummyIkController.pickUpRig.weight = elapsedTime/_pickUpDuration;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weight reaches the target value exactly
        dummyIkController.pickUpRig.weight = 1f;
    }
    
    private IEnumerator ModifyPickUpIkRigWeightToHold()
    {
        float elapsedTime = 0f;

        while (elapsedTime < itemReachDuration)
        {
            dummyIkController.pickUpRig.weight = 1 - elapsedTime/itemMoveDuration;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weight reaches the target value exactly
        dummyIkController.pickUpRig.weight = 0f;
    }
    
    /// <summary>
    /// Create a rotation of the IK target to make sure the hand reach in a natural way the item.
    /// The rotation is such that it's Y axis is aligned with the line crossing through the character shoulder and IK target.
    /// </summary>
    private void OrientTargetForHandRotation(DummyHand hand)
    {
        Vector3 armTargetDirection = hand.pickupTargetLocker.position - hand.upperArm.position;
        
        Quaternion targetRotation = Quaternion.LookRotation(armTargetDirection.normalized, Vector3.down);
        
        targetRotation *= Quaternion.AngleAxis(90f, Vector3.right);

        hand.pickupTargetLocker.rotation = targetRotation;
    }
    




  
}
