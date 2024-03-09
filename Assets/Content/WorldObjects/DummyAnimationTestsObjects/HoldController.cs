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
    
    public record HoldAndOffset(HandHoldType HandHoldType, Transform HoldTarget, Vector3 Offset, DummyHands.HandType PrimaryHand);
    
    private List<HoldAndOffset> holdData = new List<HoldAndOffset>();
    
    
    
    private void Start()
    {
        Debug.Log("start hold controller");
        pickup.OnHoldChange += HandleItemHoldChange;
       
        holdData.Add(new(HandHoldType.DoubleHandGun, gunHoldLeft,
            new Vector3(0.15f,-0.08f,0.26f), DummyHands.HandType.LeftHand));
        holdData.Add(new(HandHoldType.DoubleHandGun, gunHoldRight,
            new Vector3(-0.15f,-0.08f,0.26f), DummyHands.HandType.RightHand));
        holdData.Add(new(HandHoldType.Toolbox, toolBoxHoldLeft,
            new Vector3(-0.1f,-0.4f,0.1f), DummyHands.HandType.LeftHand));
        holdData.Add(new(HandHoldType.Toolbox, toolboxHoldRight,
            new Vector3(0.1f, -0.4f, 0.1f), DummyHands.HandType.RightHand));
        holdData.Add(new(HandHoldType.Shoulder, shoulderHoldLeft,
            new Vector3(0f, 0.18f, 0f), DummyHands.HandType.LeftHand));
        holdData.Add(new(HandHoldType.Shoulder, shoulderHoldRight,
            new Vector3(0f, 0.18f, 0f), DummyHands.HandType.RightHand));
        holdData.Add(new(HandHoldType.DoubleHandGunHarm, gunHoldHarmLeft,
            new Vector3(0f,-0.07f,0.18f), DummyHands.HandType.LeftHand));
        holdData.Add(new(HandHoldType.DoubleHandGunHarm, gunHoldHarmRight,
            new Vector3(0f,-0.07f,0.18f), DummyHands.HandType.RightHand));
    }

    // TODO add logic here so that it handles throw and pickUp and update holds accordingly.
    private void HandleItemHoldChange(bool removeItemInHand)
    {
        if(removeItemInHand && hands.UnselectedHand.itemInHand.canHoldTwoHand) 
            UpdateItemHold(hands.UnselectedHand, true);
        else if(!removeItemInHand)
            UpdateItemHold(hands.SelectedHand, false);
        
        if (hands.UnselectedHand.Full 
            && hands.UnselectedHand.itemInHand.canHoldOneHand
            && hands.UnselectedHand.itemInHand.heldWithTwoHands)
        {
            UpdateItemHold(hands.UnselectedHand, true);
        }
    }

    private void UpdateItemHold(DummyHand hand, bool alreadyInHand)
    {
        DummyItem item = hand.itemInHand;
        
        bool withTwoHands = hands.WithTwoHands(item, hand.handType, alreadyInHand);
        Transform hold = TargetFromHoldTypeAndHand(item.GetHold(withTwoHands, intents.intent), hand.handType);

        hand.SetWorldPositionRotationOfIkTarget(TargetLockerType.ItemPosition, hold);
        
        if (withTwoHands)
        {
            hands.GetOtherHand(hand.handType).SetWorldPositionRotationOfIkTarget(TargetLockerType.ItemPosition, hold);
        }
        
        SetOffsetOnItemPositionConstraint(item.GetHold(withTwoHands, intents.intent), hand.handType);
    }

    

    public Transform TargetFromHoldTypeAndHand(HandHoldType handHoldType, DummyHands.HandType selectedHand)
    {
        return holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).HoldTarget;
    }

    private Vector3 OffsetFromHoldTypeAndHand(HandHoldType handHoldType, DummyHands.HandType selectedHand)
    {
        return holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).Offset;
    }

    public void SetOffsetOnItemPositionConstraint(HandHoldType holdType, DummyHands.HandType selectedHand)
    {
        if(selectedHand == DummyHands.HandType.RightHand)
            itemRightHoldPositionIkConstraint.data.offset = OffsetFromHoldTypeAndHand(holdType, selectedHand);
        else
            itemLeftHoldPositionIkConstraint.data.offset = OffsetFromHoldTypeAndHand(holdType, selectedHand);
    }
}
