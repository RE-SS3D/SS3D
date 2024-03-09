using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Handle moving around the hold target lockers.
/// </summary>
public class HoldController : MonoBehaviour
{
    public IntentController intents;

    public DummyHands hands;

    public DummyPickUp pickup;
    
    public MultiPositionConstraint itemRightHoldPositionIkConstraint;
    
    public MultiPositionConstraint itemLeftHoldPositionIkConstraint;
    
    // Hold positions
    
    public Transform gunHoldRight;

    public Transform gunHoldLeft;

    public Transform toolboxHoldRight;

    public Transform toolBoxHoldLeft;

    public Transform shoulderHoldRight;

    public Transform shoulderHoldLeft;

    public Transform gunHoldHarmRight;

    public Transform gunHoldHarmLeft;
    
    public record HoldAndOffset(HandHoldType HandHoldType, Transform HoldTarget, Vector3 Offset, HandType PrimaryHand);
    
    private List<HoldAndOffset> _holdData = new List<HoldAndOffset>();
    
    
    
    private void Start()
    {
        Debug.Log("start hold controller");
        pickup.OnHoldChange += HandleItemHoldChange;
       
        _holdData.Add(new(HandHoldType.DoubleHandGun, gunHoldLeft,
            new Vector3(0.15f,-0.08f,0.26f), HandType.LeftHand));
        _holdData.Add(new(HandHoldType.DoubleHandGun, gunHoldRight,
            new Vector3(-0.15f,-0.08f,0.26f), HandType.RightHand));
        _holdData.Add(new(HandHoldType.Toolbox, toolBoxHoldLeft,
            new Vector3(-0.1f,-0.4f,0.1f), HandType.LeftHand));
        _holdData.Add(new(HandHoldType.Toolbox, toolboxHoldRight,
            new Vector3(0.1f, -0.4f, 0.1f), HandType.RightHand));
        _holdData.Add(new(HandHoldType.Shoulder, shoulderHoldLeft,
            new Vector3(0f, 0.18f, 0f), HandType.LeftHand));
        _holdData.Add(new(HandHoldType.Shoulder, shoulderHoldRight,
            new Vector3(0f, 0.18f, 0f),HandType.RightHand));
        _holdData.Add(new(HandHoldType.DoubleHandGunHarm, gunHoldHarmLeft,
            new Vector3(0f,-0.07f,0.18f), HandType.LeftHand));
        _holdData.Add(new(HandHoldType.DoubleHandGunHarm, gunHoldHarmRight,
            new Vector3(0f,-0.07f,0.18f), HandType.RightHand));
    }

    
    private void HandleItemHoldChange(bool removeItemInHand)
    {
        DummyHand unselectedHand = hands.UnselectedHand;
        DummyHand selectedHand = hands.SelectedHand;
        
        if (removeItemInHand && unselectedHand.Full && unselectedHand.item.canHoldTwoHand)
        {
            UpdateItemPositionConstraintAndRotation(unselectedHand);
            UpdatePickupAndHoldTargetLocker(selectedHand, true);
        }
        else
        {
            UpdateItemPositionConstraintAndRotation(selectedHand);
            UpdatePickupAndHoldTargetLocker(selectedHand, false);

            if (unselectedHand.Empty && selectedHand.item.canHoldTwoHand)
            {
                UpdatePickupAndHoldTargetLocker(unselectedHand, true);
            }

            if (unselectedHand.Full && unselectedHand.item.canHoldOneHand)
            {
                UpdateItemPositionConstraintAndRotation(unselectedHand);
            }
        }
    }

    private void UpdateItemPositionConstraintAndRotation(DummyHand hand)
    {
        DummyItem item = hand.item;
        bool withTwoHands = hands.WithTwoHands(hand);
        HandHoldType itemHoldType = item.GetHoldType(withTwoHands, intents.intent);
        Transform hold = TargetFromHoldTypeAndHand(itemHoldType, hand.handType);

        hand.SetWorldPositionRotationOfIkTarget(TargetLockerType.ItemPosition, hold);
        SetOffsetOnItemPositionConstraint(itemHoldType, hand.handType);
    }

    

    public Transform TargetFromHoldTypeAndHand(HandHoldType handHoldType, HandType selectedHand)
    {
        return _holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).HoldTarget;
    }

    private Vector3 OffsetFromHoldTypeAndHand(HandHoldType handHoldType, HandType selectedHand)
    {
        return _holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).Offset;
    }

    public void SetOffsetOnItemPositionConstraint(HandHoldType holdType, HandType selectedHand)
    {
        if(selectedHand == HandType.RightHand)
            itemRightHoldPositionIkConstraint.data.offset = OffsetFromHoldTypeAndHand(holdType, selectedHand);
        else
            itemLeftHoldPositionIkConstraint.data.offset = OffsetFromHoldTypeAndHand(holdType, selectedHand);
    }
    
    private void UpdatePickupAndHoldTargetLocker(DummyHand hand, bool secondary)
    {
        DummyItem item = secondary ? hands.GetOtherHand(hand.handType).item : hand.item;

        Transform parent = item.GetHold(!secondary, hand.handType);
        
        hand.SetParentTransformOfIkTarget(TargetLockerType.Pickup, parent);
        hand.SetParentTransformOfIkTarget(TargetLockerType.Hold,  parent);
    }
}
