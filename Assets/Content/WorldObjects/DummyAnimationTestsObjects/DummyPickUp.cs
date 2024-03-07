using SS3D.Systems.Inventory.Containers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyPickUp : MonoBehaviour
{

    public float itemMoveDuration;
    public float itemReachDuration;

    private float _pickUpDuration;

    public DummyIkController dummyIkController;

    public DummyHands hands;

    private void Start()
    {
        _pickUpDuration = itemMoveDuration + itemReachDuration;
    }
    
    private void Update()
    {

        if (!Input.GetMouseButtonDown(0))
            return;
        
        if (GetComponent<DummyHands>().IsSelectedHandEmpty)
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
        
        dummyIkController.UpdateItemHold(item, false, hands.selectedHand);
        
        GetComponent<DummyHands>().AddItemToSelectedHand(item.gameObject);

        if (!hands.IsNonSelectedHandEmpty 
            && hands.ItemInUnselectedHand.GetComponent<DummyItem>().canHoldOneHand
            && hands.ItemInUnselectedHand.GetComponent<DummyItem>().heldWithTwoHands)
        {
            dummyIkController.UpdateItemHold(hands.ItemInUnselectedHand.GetComponent<DummyItem>(), true, hands.UnselectedHand);
        }

    }

    private IEnumerator StartPickUpCoroutines(DummyItem item)
    {
        OrientTargetForHandRotation(hands.selectedHand);
        StartCoroutine(OrientPlayerTowardTarget(transform, hands.selectedHand));
        yield return ModifyPickUpIkRigWeightToReach();
        StartCoroutine(dummyIkController.MoveItemToHold(item.gameObject, itemMoveDuration, hands.selectedHand));
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
        DummyItem item = GetComponent<DummyHands>().ItemInSelectedHand.GetComponent<DummyItem>();

        item.heldWithOneHand = false;
        item.heldWithTwoHands = false;

        if (!hands.IsSelectedHandEmpty && hands.IsNonSelectedHandEmpty && item.canHoldTwoHand)
        {
            dummyIkController.rightHandHoldTwoBoneIkConstraint.weight = 0;
            dummyIkController.leftHandHoldTwoBoneIkConstraint.weight = 0;
            
        }
        else if (!hands.IsSelectedHandEmpty  && item.canHoldOneHand)
        {
            if (hands.selectedHand == DummyHands.Hand.LeftHand)
            {
                dummyIkController.leftHandHoldTwoBoneIkConstraint.weight = 0;
            }
            else
            {
                dummyIkController.rightHandHoldTwoBoneIkConstraint.weight = 0;
            }
        }

        GetComponent<DummyHands>().RemoveItemFromSelectedHand();
        
        GameObject gameObjectInUnselectedHand = GetComponent<DummyHands>().ItemInUnselectedHand;

        if (gameObjectInUnselectedHand == null)
        {
            return;
        }

        if (gameObjectInUnselectedHand.GetComponent<DummyItem>().canHoldTwoHand)
        {
            dummyIkController.UpdateItemHold(gameObjectInUnselectedHand.GetComponent<DummyItem>(),
                true, hands.UnselectedHand);
        }
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
    /// Slowly turn the character to make sure it's facing the aimed target.
    /// </summary>
    private IEnumerator OrientPlayerTowardTarget(Transform playerTransform, DummyHands.Hand hand)
    {
        float elapsedTime = 0f;
        
        // Calculate the direction from this object to the target object
        Vector3 directionFromPlayerToTarget = dummyIkController.PickUpTargetLocker(hand).position - playerTransform.position;
        
        // The y component should be 0 so the human rotate only on the XZ plane.
        directionFromPlayerToTarget.y = 0f;
        
        // Create a rotation to look in that direction
        Quaternion rotation = Quaternion.LookRotation(directionFromPlayerToTarget);

        while (elapsedTime < itemReachDuration)
        {
            // Interpolate the rotation based on the normalized time of the animation
            playerTransform.rotation = Quaternion.Lerp(playerTransform.rotation, rotation, elapsedTime/itemReachDuration);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    
    /// <summary>
    /// Create a rotation of the IK target to make sure the hand reach in a natural way the item.
    /// The rotation is such that it's Y axis is aligned with the line crossing through the character shoulder and IK target.
    /// </summary>
    private void OrientTargetForHandRotation(DummyHands.Hand hand)
    {
        Vector3 armTargetDirection = dummyIkController.PickUpTargetLocker(hand).position - dummyIkController.UpperArm(hand).position;
        
        Quaternion targetRotation = Quaternion.LookRotation(armTargetDirection.normalized, Vector3.down);
        
        targetRotation *= Quaternion.AngleAxis(90f, Vector3.right);

        dummyIkController.PickUpTargetLocker(hand).transform.rotation = targetRotation;
    }
    




  
}
