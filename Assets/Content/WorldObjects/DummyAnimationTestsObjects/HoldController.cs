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
    
    private sealed record HoldAndOffset(HandHoldType HandHoldType, Transform HoldTarget, Vector3 Offset, HandType PrimaryHand);
    
    private readonly List<HoldAndOffset> _holdData = new List<HoldAndOffset>();
    
    private void Start()
    {
        Debug.Log("start hold controller");
        pickup.OnHoldChange += HandleItemHoldChange;
        intents.OnIntentChange += HandleIntentChange;
       
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

    
    private void HandleItemHoldChange(bool removeItemInHand, DummyHand hand)
    {
        if (removeItemInHand)
        {
           HandleRemoveItem(hand);
        }
        else
        {
           HandleAddItem(hand);
        }
    }

    private void HandleRemoveItem(DummyHand hand)
    {
        DummyHand otherHand = hands.GetOtherHand(hand.handType);
        
        if (otherHand.Full && otherHand.item.canHoldTwoHand)
        {
            UpdateItemPositionConstraintAndRotation(otherHand);
            UpdatePickupAndHoldTargetLocker(hand, true);
        }
    }
    
    private void HandleAddItem(DummyHand hand)
    {
        DummyHand otherHand = hands.GetOtherHand(hand.handType);
        
        UpdateItemPositionConstraintAndRotation(hand);
        UpdatePickupAndHoldTargetLocker(hand, false);

        if (otherHand.Empty && hand.item.canHoldTwoHand)
        {
            UpdatePickupAndHoldTargetLocker(otherHand, true);
        }

        if (otherHand.Full && otherHand.item.canHoldOneHand)
        {
            UpdateItemPositionConstraintAndRotation(otherHand);
        }
    }

    private void HandleIntentChange(object sender, Intent intent)
    {
        if(hands.SelectedHand.Full)
            UpdateItemPositionConstraintAndRotation(hands.SelectedHand);
        if(hands.UnselectedHand.Full)
            UpdateItemPositionConstraintAndRotation(hands.UnselectedHand);
    }

    private void UpdateItemPositionConstraintAndRotation(DummyHand hand)
    {
        DummyItem item = hand.item;
        bool withTwoHands = hands.WithTwoHands(hand);
        HandHoldType itemHoldType = item.GetHoldType(withTwoHands, intents.intent);
        Transform hold = TargetFromHoldTypeAndHand(itemHoldType, hand.handType);

        hand.SetWorldPositionRotationTargetLocker(TargetLockerType.ItemPosition, hold);
        SetOffsetOnItemPositionConstraint(itemHoldType, hand.handType);
    }

    

    private Transform TargetFromHoldTypeAndHand(HandHoldType handHoldType, HandType selectedHand)
    {
        return _holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).HoldTarget;
    }

    private Vector3 OffsetFromHoldTypeAndHand(HandHoldType handHoldType, HandType selectedHand)
    {
        return _holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).Offset;
    }

    private void SetOffsetOnItemPositionConstraint(HandHoldType holdType, HandType selectedHand)
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
        
        hand.SetParentTransformTargetLocker(TargetLockerType.Pickup, parent);
        hand.SetParentTransformTargetLocker(TargetLockerType.Hold,  parent);
    }
}
