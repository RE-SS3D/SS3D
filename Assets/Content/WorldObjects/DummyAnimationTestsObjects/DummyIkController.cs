using SS3D.Systems.Inventory.Containers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public class DummyIkController : MonoBehaviour
{

    public DummyHands hands;

    public IntentController intents;

    // The transforms that moves to put itself on hold positions
    public Transform rightPickUpTargetLocker;
    
    public Transform leftPickUpTargetLocker;
    
    public Transform rightHoldTargetLocker;

    public Transform leftHoldTargetLocker;

    public Transform rightItemPositionTargetLocker;

    public Transform leftItemPositionTargetLocker;

    // bones 
    public Transform rightUpperArm;

    public Transform leftUpperArm;
    

    // rig stuff
    
    public Rig pickUpRig;
    
    public Rig holdRig;
    
    public TwoBoneIKConstraint rightHandHoldTwoBoneIkConstraint;
    
    public TwoBoneIKConstraint leftHandHoldTwoBoneIkConstraint;

    public MultiPositionConstraint itemRightHoldPositionIkConstraint;
    
    public MultiPositionConstraint itemLeftHoldPositionIkConstraint;
    
    public ChainIKConstraint rightArmChainIKConstraint;
    
    public ChainIKConstraint leftArmChainIKConstraint;
    
    public MultiAimConstraint headIKConstraint;


    
    // Hold positions
    
    public Transform gunHoldRight;

    public Transform gunHoldLeft;

    public Transform toolboxHoldRight;

    public Transform toolBoxHoldLeft;

    public Transform shoulderHoldRight;

    public Transform shoulderHoldLeft;

    public Transform gunHoldHarmRight;

    public Transform gunHoldHarmLeft;


    private List<HoldAndOffset> holdData = new List<HoldAndOffset>();
    
    public void Start()
    {
       
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
            new Vector3(0f, 0.18f, 0f), DummyHands.HandType.LeftHand));
        holdData.Add(new(HandHoldType.DoubleHandGunHarm, gunHoldHarmRight,
            new Vector3(0f, 0.18f, 0f), DummyHands.HandType.RightHand));
    }
    
    public record HoldAndOffset(HandHoldType HandHoldType, Transform HoldTarget, Vector3 Offset, DummyHands.HandType PrimaryHand);

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
    


    public void UpdateItemHold(DummyItem item, bool alreadyInHand, DummyHand hand)
    {
         bool withTwoHands = SetConstraintsWeights(item, hand.handType, alreadyInHand);

        Transform hold = TargetFromHoldTypeAndHand(item.GetHold(withTwoHands, intents.intent), hand.handType);

        hand.SetWorldPositionRotationOfIkTarget(TargetLockerType.ItemPosition, hold);
        
        if (withTwoHands)
        {
            hands.GetOtherHand(hand.handType).SetWorldPositionRotationOfIkTarget(TargetLockerType.ItemPosition, hold);
        }
        
        SetOffsetOnItemPositionConstraint(withTwoHands ? item.twoHandHold : item.singleHandHold, hand.handType);
        
        MoveIkTargets(item, withTwoHands, hand);
    }

    private bool SetConstraintsWeights(DummyItem item, DummyHands.HandType hand, bool alreadyInHand)
    {
        bool withTwoHands = false;
        
        if ((hands.BothHandEmpty || (!hands.GetOtherHand(hand).Full && alreadyInHand))  && item.canHoldTwoHand)
        {
            rightArmChainIKConstraint.weight = 1;
            leftArmChainIKConstraint.weight = 1;
            rightHandHoldTwoBoneIkConstraint.weight = 1;
            leftHandHoldTwoBoneIkConstraint.weight = 1;
            withTwoHands = true;
            item.heldWithTwoHands = true;
        }
        else if (item.canHoldOneHand)
        {
            item.heldWithOneHand = true;
            
            if (hand == DummyHands.HandType.LeftHand)
            {
                rightArmChainIKConstraint.weight = 0;
                leftArmChainIKConstraint.weight = 1;
                leftHandHoldTwoBoneIkConstraint.weight = 1;
               
            }
            else
            {
                rightArmChainIKConstraint.weight = 1;
                leftArmChainIKConstraint.weight = 0;
                rightHandHoldTwoBoneIkConstraint.weight = 1;
            }
        }

        return withTwoHands;
    }
    
    private void MoveIkTargets(DummyItem item, bool withTwoHands, DummyHand hand)
    {
        Transform primaryParent = hand.handType == DummyHands.HandType.RightHand ?
            item.primaryRightHandHold.transform : item.primaryLeftHandHold.transform;
        
        Transform secondaryParent = hand.handType == DummyHands.HandType.RightHand ?
            item.secondaryLeftHandHold.transform : item.secondaryRightHandHold.transform;
        
        hand.SetParentTransformOfIkTarget(TargetLockerType.Pickup,  primaryParent);
        hand.SetParentTransformOfIkTarget(TargetLockerType.Hold,  primaryParent);

        if (withTwoHands)
        {
            hands.GetOtherHand(hand.handType).SetParentTransformOfIkTarget(TargetLockerType.Pickup, secondaryParent);
            hands.GetOtherHand(hand.handType).SetParentTransformOfIkTarget(TargetLockerType.Hold, secondaryParent);
        }

    }
}
