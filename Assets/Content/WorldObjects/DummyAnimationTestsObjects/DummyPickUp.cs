using System.Collections;
using UnityEngine;

public delegate void Notify(bool removeItem, DummyHand hand);

public class DummyPickUp : MonoBehaviour
{

    public float itemMoveDuration;
    public float itemReachDuration;
    
    public DummyIkController dummyIkController;

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
            TryThrow();
        }

    }

    private void PickUp(DummyItem item)
    {
        GetComponent<DummyAnimatorController>().TriggerPickUp();

        StartCoroutine(StartPickUpCoroutines(item));
  
        hands.SelectedHand.AddItem(item);
        
        OnHoldChange?.Invoke(false, hands.SelectedHand);
    }

    private IEnumerator StartPickUpCoroutines(DummyItem item)
    {
        OrientTargetForHandRotation(hands.SelectedHand);
        
        StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(
            transform, item.transform, itemReachDuration, false, true));
        
        yield return CoroutineHelper.ModifyValueOverTime(x => dummyIkController.pickUpRig.weight = x,
            0f, 1f, itemReachDuration);
        
        StartCoroutine(DummyTransformHelper.LerpTransform(item.transform,
            hands.SelectedHand.itemPositionTargetLocker, itemMoveDuration));
        
        yield return CoroutineHelper.ModifyValueOverTime(x => dummyIkController.pickUpRig.weight = x,
            1f, 0f, itemMoveDuration);
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
 
        hands.SelectedHand.RemoveItem();

        OnHoldChange?.Invoke(true, hands.SelectedHand);
    }

    private IEnumerator StartDropCoroutines()
    {
        yield return null;
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
