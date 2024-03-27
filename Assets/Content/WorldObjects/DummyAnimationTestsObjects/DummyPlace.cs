using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class DummyPlace : MonoBehaviour
{

    public DummyHands hands;

    public float itemReachDuration;
    public float itemMoveDuration;

    public HoldController holdController;
    public Transform lookAtTargetLocker;
    public MultiAimConstraint lookAtConstraint;
    
    public Transform hips;
    
    public bool UnderMaxDistanceFromHips(Vector3 position) => Vector3.Distance(hips.position, position) < 1.3f;
    
    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;
        
        if (hands.SelectedHand.Full)
        {
            TryPlace();
        }
    }
    
    private void TryPlace()
    {
        // Cast a ray from the mouse position into the scene
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Check if the ray hits any collider
        if (Physics.Raycast(ray, out RaycastHit hit) && UnderMaxDistanceFromHips(hit.point))
        {
            StartCoroutine(Place(hit.point));
        }
    }

    private IEnumerator Place(Vector3 placePosition)
    {
        DummyHand mainHand = hands.SelectedHand;
        DummyHand secondaryHand = hands.GetOtherHand(mainHand.handType);
        bool withTwoHands = secondaryHand.Empty && hands.SelectedHand.item.canHoldTwoHand;
        Transform placeTarget = mainHand.placeTarget;
        DummyItem item = mainHand.item;
        
        SetupPlace(placePosition, item, mainHand, secondaryHand, withTwoHands);

        yield return PlaceReach(mainHand, placeTarget, item);

        yield return PlaceAndPullBack(mainHand, secondaryHand, withTwoHands);

    }

    private void SetupPlace(Vector3 placePosition, DummyItem item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
    {
        // Set up the position the item should be placed on
        hands.SelectedHand.placeTarget.position = placePosition + 0.2f*Vector3.up;

        // Unparent item so its not constrained by the multi-position constrain anymore.
        item.transform.parent = null;

        // set pickup constraint to 1 so that the player can bend to reach at its feet or further in front.
        mainHand.pickupIkConstraint.weight = 1f;
        
        // Remove hold constraint from second hand if item held with two hands.
        if (withTwoHands)
        {
            secondaryHand.pickupIkConstraint.weight = 1f;
        }
        
        // Place look at target at place item position
        lookAtTargetLocker.transform.parent = null;
        lookAtTargetLocker.position = placePosition;
    }

    private IEnumerator PlaceReach(DummyHand mainHand, Transform placeTarget, DummyItem item)
    {
        // Turn character toward the position to place the item.
        if (GetComponent<DummyPositionController>().Position != PositionType.Sitting)
        {
            StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(transform,
                placeTarget, itemReachDuration, false, true));
        }

        if (mainHand.handBone.transform.position.y - placeTarget.position.y > 0.3)
        {
            GetComponent<DummyAnimatorController>().Crouch(true);
        }
        
        // Slowly increase looking at place item position
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight= x,
            0f, 1f, itemReachDuration));

        // Slowly move item toward the position it should be placed.
        yield return DummyTransformHelper.LerpTransform(item.transform, placeTarget,
            itemMoveDuration, true, false, false);
    }
    
    private IEnumerator PlaceAndPullBack(DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
    {
        GetComponent<DummyAnimatorController>().Crouch(false);
        
        mainHand.RemoveItem();
        
        mainHand.pickupTargetLocker.parent = null;

        // Slowly decrease main hand pick up constraint so player stop reaching for pickup target
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => mainHand.pickupIkConstraint.weight = x,
            1f, 0f, itemReachDuration));
        
        // Slowly stop looking at item place position
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight= x,
            1f, 0f, itemReachDuration));

        // reproduce changes on second hand
        if (withTwoHands)
        {
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => secondaryHand.pickupIkConstraint.weight = x,
                1f, 0f, itemReachDuration));
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => secondaryHand.holdIkConstraint.weight = x,
                1f, 0f, itemReachDuration));
        }
        
        // Slowly stop trying to hold item
        yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.holdIkConstraint.weight = x,
             1f, 0f, itemReachDuration);
        
        // Catch two hands holdable item in other hand with main hand, just freed.
        if (secondaryHand.Full && secondaryHand.item.canHoldTwoHand)
        {
            holdController.UpdateItemPositionConstraintAndRotation(secondaryHand, true, itemReachDuration, false);
            holdController.MovePickupAndHoldTargetLocker(mainHand, true);
            yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.holdIkConstraint.weight = x,
                0f, 1f, itemReachDuration/2);
        }
    }
}
