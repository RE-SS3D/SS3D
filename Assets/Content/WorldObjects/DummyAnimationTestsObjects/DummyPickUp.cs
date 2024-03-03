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

        if (hands.BothHandEmpty && item.canHoldTwoHand)
        {
            dummyIkController.rightArmChainIKConstraint.weight = 1;
            dummyIkController.leftArmChainIKConstraint.weight = 1;
            dummyIkController.rightHandHoldTwoBoneIkConstraint.weight = 1;
            dummyIkController.leftHandHoldTwoBoneIkConstraint.weight = 1;
            withTwoHands = true;
        }
        else if (hands.IsSelectedHandEmpty  && item.canHoldOneHand)
        {
            if (hands.selectedHand == DummyHands.Hand.LeftHand)
            {
                dummyIkController.rightArmChainIKConstraint.weight = 0;
                dummyIkController.leftArmChainIKConstraint.weight = 1;
                dummyIkController.leftHandHoldTwoBoneIkConstraint.weight = 1;
            }
            else
            {
                dummyIkController.rightArmChainIKConstraint.weight = 1;
                dummyIkController.leftArmChainIKConstraint.weight = 0;
                dummyIkController.rightHandHoldTwoBoneIkConstraint.weight = 1;
            }
        }
        else
        {
            return;
        }

        Transform hold = dummyIkController.TargetFromHoldTypeAndHand(withTwoHands ? item.twoHandHold : item.singleHandHold, hands.selectedHand);

        if (withTwoHands)
        {
            dummyIkController.SetWorldPositionRotationOfIkTargets(DummyIkController.IkTargetType.Hold, hold);
        }
        else if(hands.selectedHand == DummyHands.Hand.LeftHand)
        {
            dummyIkController.SetWorldPositionRotationOfIkTarget(DummyIkController.IkTargetType.Hold,
                DummyHands.Hand.LeftHand, hold);
        }
        else
        {
            dummyIkController.SetWorldPositionRotationOfIkTarget(DummyIkController.IkTargetType.Hold,
                DummyHands.Hand.RightHand, hold);
        }
        

        dummyIkController.SetOffsetOnItemPositionConstraint(withTwoHands ? item.twoHandHold : item.singleHandHold, hands.selectedHand);
        
        
        MoveIkTargets(item, hands.selectedHand, withTwoHands);
        
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
    }
    
    private IEnumerator MoveItemToHold(GameObject item)
    {
        Vector3 initialPosition = item.transform.position;
        Quaternion initialRotation = item.transform.rotation;
        float timer = 0.0f;
        Transform targetHold = dummyIkController.SelectedHandHoldIkTarget;

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

    private void MoveIkTargets(DummyItem item, DummyHands.Hand selectedHand, bool withTwoHands)
    {
        Transform primaryParent = selectedHand == DummyHands.Hand.RightHand ?
            item.primaryRightHandHold.transform : item.primaryLeftHandHold.transform;
        
        Transform secondaryParent = selectedHand == DummyHands.Hand.RightHand ?
            item.secondaryLeftHandHold.transform : item.secondaryRightHandHold.transform;
        
        dummyIkController.SetParentTransformOfIkTarget(DummyIkController.IkTargetType.Pickup,
            DummyHands.Hand.RightHand,  primaryParent);
        dummyIkController.SetParentTransformOfIkTarget(DummyIkController.IkTargetType.Hold,
            DummyHands.Hand.RightHand,  primaryParent);

        if (withTwoHands)
        {
            dummyIkController.SetParentTransformOfIkTarget(DummyIkController.IkTargetType.Pickup,
                DummyHands.Hand.LeftHand,  secondaryParent);
            dummyIkController.SetParentTransformOfIkTarget(DummyIkController.IkTargetType.Hold,
                DummyHands.Hand.LeftHand,  secondaryParent);
        }

    }

  
}
