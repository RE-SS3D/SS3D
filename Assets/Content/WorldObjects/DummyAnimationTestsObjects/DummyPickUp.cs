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

        StartCoroutine(ModifyPickUpIkRigWeight());
        
        dummyIkController.UpdateItemHold(item, false, hands.selectedHand);
        
        GetComponent<DummyHands>().AddItemToSelectedHand(item.gameObject);

        if (!hands.IsNonSelectedHandEmpty 
            && hands.ItemInUnselectedHand.GetComponent<DummyItem>().canHoldOneHand
            && hands.ItemInUnselectedHand.GetComponent<DummyItem>().heldWithTwoHands)
        {
            dummyIkController.UpdateItemHold(hands.ItemInUnselectedHand.GetComponent<DummyItem>(), true, hands.UnselectedHand);
        }

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
    
    
    
    private IEnumerator ModifyPickUpIkRigWeight()
    {
        
        float elapsedTime = 0f;

        while (elapsedTime < _pickUpDuration)
        {
            float currentLoopNormalizedTime = elapsedTime/_pickUpDuration;

            if (currentLoopNormalizedTime < 0.5f) {  
                dummyIkController.pickUpRig.weight = currentLoopNormalizedTime *2;
            }
            else
            {
                dummyIkController.pickUpRig.weight = 2f - 2f * currentLoopNormalizedTime;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weight reaches the target value exactly
        dummyIkController.pickUpRig.weight = 0f;
    }
    




  
}
