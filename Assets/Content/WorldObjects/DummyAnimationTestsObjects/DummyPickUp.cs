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
        bool withTwoHands = false;

        if (!hands.leftHandFull && !hands.rightHandFull && item.canHoldTwoHand)
        {
            dummyIkController.rightHandHoldTwoBoneIkConstraint.weight = 1;
            dummyIkController.leftHandHoldTwoBoneIkConstraint.weight = 1;
            withTwoHands = true;
        }
        else if (hands.IsSelectedHandEmpty  && item.canHoldOneHand)
        {
            if (hands.selectedHand == DummyHands.Hand.LeftHand)
            {
                dummyIkController.rightHandHoldTwoBoneIkConstraint.weight = 0;
                dummyIkController.leftHandHoldTwoBoneIkConstraint.weight = 1;
            }
            else
            {
                dummyIkController.rightHandHoldTwoBoneIkConstraint.weight = 1;
                dummyIkController.leftHandHoldTwoBoneIkConstraint.weight = 0;
            }
        }
        else
        {
            return;
        }

        Transform hold = dummyIkController.TargetFromHoldTypeAndHand(item.singleHandHold, item.twoHandHold, withTwoHands, hands.selectedHand);
        dummyIkController.holdPositionTarget.position = hold.position;
        dummyIkController.holdPositionTarget.rotation = hold.rotation;

        dummyIkController.SetOffsetOnItemPositionConstraint(hold, hands.selectedHand == DummyHands.Hand.RightHand);
        
        
        MoveIkTargets(item, hands.selectedHand);
        
        GetComponent<DummyAnimatorController>().TriggerPickUp();

        StartCoroutine(ModifyPickUpIkRigWeight());
        
      


        StartCoroutine(PutItemInHand(item));
    }

    private IEnumerator PutItemInHand(DummyItem item)
    {
        yield return new WaitForSeconds(itemReachDuration);
        
        StartCoroutine(MoveItemToHold(item.gameObject));
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
        GetComponent<DummyHands>().RemoveItemFromSelectedHand();
        dummyIkController.holdRig.weight = 0f;
    }
    
    private IEnumerator MoveItemToHold(GameObject item)
    {
        Vector3 initialPosition = item.transform.position;
        Quaternion initialRotation = item.transform.rotation;
        float timer = 0.0f;

        Transform targetHold = dummyIkController.holdPositionTarget;

        while (timer < itemMoveDuration)
        {
            float t = timer / itemMoveDuration;
            item.transform.position = Vector3.Lerp(initialPosition, targetHold.position, t);
            item.transform.rotation = Quaternion.Lerp(initialRotation, targetHold.rotation, t);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final transform matches the target values exactly
        item.transform.position = targetHold.position;
        item.transform.rotation = targetHold.rotation;

        item.transform.parent = targetHold;
        
        
        GetComponent<DummyHands>().AddItemToSelectedHand(item.gameObject);
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

    private void MoveIkTargets(DummyItem item, DummyHands.Hand selectedHand)
    {
        if (selectedHand == DummyHands.Hand.RightHand)
        {
            dummyIkController.rightHandPickUpIkTarget.transform.parent = item.primaryRightHandHold.transform;
            dummyIkController.rightHandPickUpIkTarget.transform.localPosition = Vector3.zero;
        
            dummyIkController.leftHandPickUpIkTarget.transform.parent = item.secondaryLeftHandHold.transform;
            dummyIkController.leftHandPickUpIkTarget.transform.localPosition = Vector3.zero;
        
            dummyIkController.rightHandHoldIkTarget.transform.parent = item.primaryRightHandHold.transform;
            dummyIkController.rightHandHoldIkTarget.transform.localPosition = Vector3.zero;
            dummyIkController.rightHandHoldIkTarget.transform.localRotation = Quaternion.identity;
        
            dummyIkController.leftHandHoldIkTarget.transform.parent = item.secondaryLeftHandHold.transform;
            dummyIkController.leftHandHoldIkTarget.transform.localPosition = Vector3.zero;
            dummyIkController.leftHandHoldIkTarget.transform.localRotation = Quaternion.identity;
        }
        else
        {
            dummyIkController.rightHandPickUpIkTarget.transform.parent = item.secondaryRightHandHold.transform;
            dummyIkController.rightHandPickUpIkTarget.transform.localPosition = Vector3.zero;
        
            dummyIkController.leftHandPickUpIkTarget.transform.parent = item.primaryLeftHandHold.transform;
            dummyIkController.leftHandPickUpIkTarget.transform.localPosition = Vector3.zero;
        
            dummyIkController.rightHandHoldIkTarget.transform.parent = item.secondaryRightHandHold.transform;
            dummyIkController.rightHandHoldIkTarget.transform.localPosition = Vector3.zero;
            dummyIkController.rightHandHoldIkTarget.transform.localRotation = Quaternion.identity;
        
            dummyIkController.leftHandHoldIkTarget.transform.parent = item.primaryLeftHandHold.transform;
            dummyIkController.leftHandHoldIkTarget.transform.localPosition = Vector3.zero;
            dummyIkController.leftHandHoldIkTarget.transform.localRotation = Quaternion.identity;
        }

    }

  
}
