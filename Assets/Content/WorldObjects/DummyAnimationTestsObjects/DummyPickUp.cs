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
        holdController.UpdateItemPositionConstraintAndRotation(mainHand, withTwoHands, 0f, false);

        // Needed to constrain item to position, in case the weight has been changed elsewhere
        mainHand.itemPositionConstraint.weight = 1f;
        
        // Place pickup and hold target lockers on the item, at their respective position and rotation.
        holdController.MovePickupAndHoldTargetLocker(mainHand, false);

        // Orient hand in a natural position to reach for item.
        OrientTargetForHandRotation(mainHand);
        
        // Needed if this has been changed elsewhere 
        mainHand.pickupIkConstraint.data.tipRotationWeight = 1f;
        
        // Needed as the hand need to reach when picking up in an extended position, it looks unnatural
        // if it takes directly the rotation of the hold.
        mainHand.holdIkConstraint.data.targetRotationWeight = 0f;

        // Reproduce changes on secondary hand if necessary.
        if (withTwoHands)
        {
            holdController.MovePickupAndHoldTargetLocker(secondaryHand, true);
            OrientTargetForHandRotation(secondaryHand);
            secondaryHand.pickupIkConstraint.data.tipRotationWeight = 1f;
            secondaryHand.holdIkConstraint.data.targetRotationWeight = 0f;
        }
        
        // Set up the look at target locker on the item to pick up.
        lookAtTargetLocker.transform.parent = item.transform;
        lookAtTargetLocker.localPosition = Vector3.zero;
        lookAtTargetLocker.localRotation = Quaternion.identity;
    }

    private IEnumerator PickupReach(DummyItem item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
    {
        // Move player toward item
        if (GetComponent<DummyPositionController>().Position != PositionType.Sitting)
        {
            StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(transform,
                item.transform, itemReachDuration, false, true));
        }

        if (mainHand.handBone.transform.position.y - item.transform.position.y > 0.3)
        {
            GetComponent<DummyAnimatorController>().Crouch(true);

            yield return new WaitForSeconds(0.25f);
        }

        // Change hold constraint weight of the main hand from 0 to 1
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(
            x => mainHand.holdIkConstraint.weight = x, 0f, 1f, itemReachDuration));
        
        // Start looking at item
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight= x,
            0f, 1f, itemReachDuration));

        // Reproduce changes on second hand if picking up with two hands
        if (withTwoHands)
        {
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(
                x => secondaryHand.holdIkConstraint.weight = x, 0f, 1f, itemReachDuration));
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => secondaryHand.pickupIkConstraint.weight = x,
                0f, 1f, itemReachDuration));
        }
        
        // Change pickup constraint weight of the main hand from 0 to 1    
        yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.pickupIkConstraint.weight = x,
            0f, 1f, itemReachDuration);
    }

    private IEnumerator PickupPullBack(DummyItem item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
    {
        GetComponent<DummyAnimatorController>().Crouch(false);
        
        // Move item toward its constrained position.
        StartCoroutine(DummyTransformHelper.LerpTransform(item.transform,
            hands.SelectedHand.itemPositionTargetLocker, itemMoveDuration));

        // if an item held with two hands, change it with a single hand hold
        if (secondaryHand.Full && secondaryHand.item.canHoldTwoHand)
        {
            holdController.UpdateItemPositionConstraintAndRotation(secondaryHand, false, itemMoveDuration, false);
        }
        
        // Stop looking at item         
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight= x,
            1f, 0f, itemReachDuration));
        
        // increase hold constraint rotation
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => mainHand.holdIkConstraint.data.targetRotationWeight= x,
           0f, 1f, itemReachDuration));

        if (withTwoHands)
        {
           StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => secondaryHand.holdIkConstraint.data.targetRotationWeight= x,
                0f, 1f, itemReachDuration));
        }
        
        // Get hand back at its hold position.
        if (withTwoHands)
        {
            StartCoroutine( CoroutineHelper.ModifyValueOverTime(x => secondaryHand.pickupIkConstraint.weight = x,
                1f, 0f, itemMoveDuration));
        }
        yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.pickupIkConstraint.weight = x,
            1f, 0f, itemMoveDuration);
        
        // Place item on constrained item position
        item.transform.parent = mainHand.itemPositionTargetLocker;
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        
        lookAtConstraint.weight = 0f;
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
