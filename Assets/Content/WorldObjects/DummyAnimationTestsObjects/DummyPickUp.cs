using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public delegate void Notify(bool removeItem, DummyHand hand);

public class DummyPickUp : MonoBehaviour
{

    public float itemMoveDuration;
    public float itemReachDuration;

    public HoldController holdController;

    public DummyHands hands;

    public Transform hips;

    public MultiAimConstraint lookAtConstraint;

    public Transform lookAtTargetLocker;
    

    public bool UnderMaxDistanceFromHips(Vector3 position) => Vector3.Distance(hips.position, position) < 1.3f;
    
    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;
        
        if (hands.SelectedHand.Empty)
        {
            TryPickUp();
        }
        else
        {
            TryPlace();
        }

    }
    
    private void TryPickUp()
    {
        // Cast a ray from the mouse position into the scene
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Check if the ray hits any collider
        if (Physics.Raycast(ray, out RaycastHit hit)&& UnderMaxDistanceFromHips(hit.point))
        {
            // Check if the collider belongs to a GameObject
            GameObject obj = hit.collider.gameObject;

            // should add conditions to check other objects doesn't require two hands.
            // also check picked up object doesn't require two hands if other hand is full.
            if (obj.TryGetComponent(out DummyItem item)) 
            {
                StartCoroutine(PickUp(item));
            }
        }
    }

    private IEnumerator PickUp(DummyItem item)
    {
        GetComponent<DummyAnimatorController>().TriggerPickUp();
        
        DummyHand secondaryHand = hands.GetOtherHand(hands.SelectedHand.handType);

        bool withTwoHands = secondaryHand.Empty && item.canHoldTwoHand;
        
        hands.SelectedHand.AddItem(item);

        SetUpPickup(hands.SelectedHand, secondaryHand, withTwoHands, item);

        yield return PickupReach(item, hands.SelectedHand, secondaryHand, withTwoHands);

        yield return PickupPullBack(item, hands.SelectedHand, secondaryHand, withTwoHands);
    }

    private void SetUpPickup(DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands, DummyItem item)
    {
        holdController.UpdateItemPositionConstraintAndRotation(mainHand, withTwoHands);

        mainHand.itemPositionConstraint.weight = 1f;
        
        holdController.MovePickupAndHoldTargetLocker(mainHand, false);
        if(withTwoHands)
            holdController.MovePickupAndHoldTargetLocker(secondaryHand, true);

        lookAtTargetLocker.transform.parent = item.transform;
        lookAtTargetLocker.localPosition = Vector3.zero;
        lookAtTargetLocker.localRotation = Quaternion.identity;
    }

    private IEnumerator PickupReach(DummyItem item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
    {
        StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(
            transform, item.transform, itemReachDuration, false, true));

        StartCoroutine(CoroutineHelper.ModifyValueOverTime(
            x => mainHand.holdIkConstraint.weight = x, 0f, 1f, itemReachDuration));

        if (withTwoHands)
        {
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(
                x => secondaryHand.pickupIkConstraint.weight = x, 0f, 1f, itemReachDuration));
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(
                x => secondaryHand.holdIkConstraint.weight = x, 0f, 1f, itemReachDuration));
        }
        
        // Start looking at item
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight= x,
            0f, 1f, itemReachDuration));
            
        yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.pickupIkConstraint.weight = x,
            0f, 1f, itemReachDuration);
    }

    private IEnumerator PickupPullBack(DummyItem item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
    {
        // Move item toward its constrained position.
        StartCoroutine(DummyTransformHelper.LerpTransform(item.transform,
            hands.SelectedHand.itemPositionTargetLocker, itemMoveDuration));

        if (secondaryHand.Full && secondaryHand.item.canHoldTwoHand)
        {
            holdController.UpdateItemPositionConstraintAndRotation(secondaryHand, false);
        }
        
        // Stop looking at item         
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight= x,
            1f, 0f, itemReachDuration));
        
        // Get hands back at their hold position.
        if (withTwoHands)
        {
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(
                x => secondaryHand.pickupIkConstraint.weight = x, 1f, 0f, itemReachDuration));
        }
        
        yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.pickupIkConstraint.weight = x,
            1f, 0f, itemMoveDuration);
        
        // Place item on constrained item position
        item.transform.parent = mainHand.itemPositionTargetLocker;
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        
        lookAtConstraint.weight = 0f;
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

        yield return ItemPlaceReach(placeTarget, item);

        yield return PlaceAndPullBack(mainHand);

    }

    private void SetupPlace(Vector3 placePosition, DummyItem item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
    {
        hands.SelectedHand.placeTarget.position = placePosition + 0.2f*Vector3.up;

        item.transform.parent = null;

        mainHand.pickupIkConstraint.weight = 1f;
        
        if (withTwoHands)
        {
            secondaryHand.holdIkConstraint.weight = 0f;
        }
        
        lookAtTargetLocker.transform.parent = null;
        lookAtTargetLocker.position = placePosition;
    }

    private IEnumerator ItemPlaceReach(Transform placeTarget, DummyItem item)
    {
        StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(
            transform, placeTarget, itemReachDuration, false, true));
        
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight= x,
            0f, 1f, itemReachDuration));
        

        yield return DummyTransformHelper.LerpTransform(item.transform, placeTarget,
            itemMoveDuration, true, false, false);
    }
    
    private IEnumerator PlaceAndPullBack(DummyHand mainHand)
    {
        mainHand.RemoveItem();
        
        hands.SelectedHand.pickupTargetLocker.parent = null;

        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => mainHand.pickupIkConstraint.weight = x,
            1f, 0f, itemReachDuration));
        
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight= x,
            1f, 0f, itemReachDuration));
        
        yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.holdIkConstraint.weight = x,
            1f, 0f, itemReachDuration);
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
