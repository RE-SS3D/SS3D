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
        
        dummyIkController.UpdateItemHold(item, false);

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
            dummyIkController.UpdateItemHold(gameObjectInUnselectedHand.GetComponent<DummyItem>(), true);
        }
    }
    
    
    
    private IEnumerator ModifyPickUpIkRigWeight()
    {
        
        float elapsedTime = 0f;
        bool startHolding = false;

        while (elapsedTime < _pickUpDuration)
        {
            float currentLoopNormalizedTime = elapsedTime/_pickUpDuration;

            if (currentLoopNormalizedTime < 0.5f) {  
                dummyIkController.pickUpRig.weight = currentLoopNormalizedTime *2;
            }
            else
            {
                if (!startHolding)
                {
                    StartCoroutine(ModifyHoldIkRigWeight());
                    startHolding = true;
                }
                dummyIkController.pickUpRig.weight = 2f - 2f * currentLoopNormalizedTime;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weight reaches the target value exactly
        dummyIkController.pickUpRig.weight = 0f;
    }
    
    private IEnumerator ModifyHoldIkRigWeight()
    {
        
        float elapsedTime = 0f;

        while (elapsedTime < itemMoveDuration)
        {
            float weight = elapsedTime/itemMoveDuration;

            dummyIkController.holdRig.weight = weight;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weight reaches the target value exactly
        dummyIkController.holdRig.weight = 1f;
    }



  
}
