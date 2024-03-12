using System.Collections;
using UnityEngine;

public delegate void Notify(bool removeItem, DummyHand hand);

public class DummyPickUp : MonoBehaviour
{

    public float itemMoveDuration;
    public float itemReachDuration;
    
    public DummyIkController dummyIkController;

    public HoldController holdController;

    public DummyHands hands;

    public event Notify OnHoldChange;
    
    
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

    private void PickUp(DummyItem item)
    {
        GetComponent<DummyAnimatorController>().TriggerPickUp();
        
        DummyHand secondaryHand = hands.GetOtherHand(hands.SelectedHand.handType);

        bool withTwoHands = secondaryHand.Empty && item.canHoldTwoHand;
        
        hands.SelectedHand.AddItem(item);

        StartCoroutine(StartPickUpCoroutines(item, hands.SelectedHand, withTwoHands));
    }

    private IEnumerator StartPickUpCoroutines(DummyItem item, DummyHand mainHand, bool withTwoHands)
    {
        holdController.UpdateItemPositionConstraintAndRotation(mainHand, withTwoHands);

        mainHand.itemPositionConstraint.weight = 1f;
        
        DummyHand secondaryHand = hands.GetOtherHand(mainHand.handType);
        
        OrientTargetForHandRotation(hands.SelectedHand);

        holdController.MovePickupAndHoldTargetLocker(mainHand, false);
        if(withTwoHands)
            holdController.MovePickupAndHoldTargetLocker(secondaryHand, true);
        
        // turn toward targets and reach
        
        StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(
            transform, item.transform, itemReachDuration, false, true));

        // increase pickup and hold constraint.
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(
            x => mainHand.holdIkConstraint.weight = x, 0f, 1f, itemReachDuration));
        if(withTwoHands)
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(
                x => secondaryHand.holdIkConstraint.weight = x, 0f, 1f, itemReachDuration));
            
        if (withTwoHands)
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(
                x => secondaryHand.pickupIkConstraint.weight = x, 0f, 1f, itemReachDuration));
        yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.pickupIkConstraint.weight = x,
            0f, 1f, itemReachDuration);
       
            
        // item reach at this point.
        StartCoroutine(DummyTransformHelper.LerpTransform(item.transform,
            hands.SelectedHand.itemPositionTargetLocker, itemMoveDuration));

        if (secondaryHand.Full && secondaryHand.item.canHoldTwoHand)
        {
            holdController.UpdateItemPositionConstraintAndRotation(secondaryHand, false);
        }
        
        // Get hands back at their hold position.
        yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.pickupIkConstraint.weight = x,
            1f, 0f, itemMoveDuration);
        
        if (withTwoHands)
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(
                x => secondaryHand.pickupIkConstraint.weight = x, 1f, 0f, itemReachDuration));

        item.transform.parent = mainHand.itemPositionTargetLocker;
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        
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

    private void TryPlace()
    {
        // Cast a ray from the mouse position into the scene
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Check if the ray hits any collider
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if the collider belongs to a GameObject
            GameObject obj = hit.collider.gameObject;
            
            DummyHand secondaryHand = hands.GetOtherHand(hands.SelectedHand.handType);
            
            bool withTwoHands = secondaryHand.Empty && hands.SelectedHand.item.canHoldTwoHand;

            hands.SelectedHand.pickupTargetLocker.parent = null;
            hands.SelectedHand.pickupTargetLocker.position = hit.point + 0.2f*Vector3.up;
            
            StartCoroutine(StartPlaceCoroutines(hands.SelectedHand, withTwoHands));
        }
    }

    private IEnumerator StartPlaceCoroutines(DummyHand mainHand, bool withTwoHands)
    {
        DummyHand secondaryHand = hands.GetOtherHand(mainHand.handType);
        
        Transform dropTarget = mainHand.pickupTargetLocker;

        DummyItem item = mainHand.item;

        item.transform.parent = mainHand.handBone; 
        
        OrientTargetForHandRotation(hands.SelectedHand);
        
        StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(
            transform, dropTarget, itemReachDuration, false, true));
        
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(
            x => mainHand.holdIkConstraint.weight = x, 1f, 0f, itemReachDuration));
        
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(
            x => mainHand.itemPositionConstraint.weight = x, 1f, 0f, itemReachDuration));
        
        if (withTwoHands)
        {
            secondaryHand.holdIkConstraint.weight = 0f;
        }
        
        yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.pickupIkConstraint.weight = x,
            0f, 1f, itemReachDuration);

       
        
        mainHand.RemoveItem();

        item.transform.position = mainHand.pickupTargetLocker.position;
        

        yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.pickupIkConstraint.weight = x,
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
