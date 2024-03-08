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
    
    public enum TargetLockerType
    {
        Pickup,
        Hold,
        ItemPosition,
    }

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


    private List<HoldAndOffset> holdData = new List<HoldAndOffset>();
    
    public void Start()
    {
        holdData.Add(new(DummyItem.HandHoldType.DoubleHandGun, gunHoldLeft,
            new Vector3(0.15f,-0.08f,0.26f), DummyHands.HandType.LeftHand));
        holdData.Add(new(DummyItem.HandHoldType.DoubleHandGun, gunHoldRight,
            new Vector3(-0.15f,-0.08f,0.26f), DummyHands.HandType.RightHand));
        holdData.Add(new(DummyItem.HandHoldType.Toolbox, toolBoxHoldLeft,
            new Vector3(0.06f,-0.64f,0.11f), DummyHands.HandType.LeftHand));
        holdData.Add(new(DummyItem.HandHoldType.Toolbox, toolboxHoldRight,
            new Vector3(-0.06f, -0.64f, 0.11f), DummyHands.HandType.RightHand));
        holdData.Add(new(DummyItem.HandHoldType.Shoulder, shoulderHoldLeft,
            new Vector3(0f, 0.18f, 0f), DummyHands.HandType.LeftHand));
        holdData.Add(new(DummyItem.HandHoldType.Shoulder, shoulderHoldRight,
            new Vector3(0f, 0.18f, 0f), DummyHands.HandType.RightHand));
    }
    
    public record HoldAndOffset(DummyItem.HandHoldType HandHoldType, Transform HoldTarget, Vector3 Offset, DummyHands.HandType PrimaryHand);

    public Transform TargetFromHoldTypeAndHand(DummyItem.HandHoldType handHoldType, DummyHands.HandType selectedHand)
    {
        return holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).HoldTarget;
    }

    private Vector3 OffsetFromHoldTypeAndHand(DummyItem.HandHoldType handHoldType, DummyHands.HandType selectedHand)
    {
        return holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).Offset;
    }

    public void SetOffsetOnItemPositionConstraint(DummyItem.HandHoldType holdType, DummyHands.HandType selectedHand)
    {
        if(selectedHand == DummyHands.HandType.RightHand)
            itemRightHoldPositionIkConstraint.data.offset = OffsetFromHoldTypeAndHand(holdType, selectedHand);
        else
            itemLeftHoldPositionIkConstraint.data.offset = OffsetFromHoldTypeAndHand(holdType, selectedHand);
    }
    
    public void SetWorldPositionRotationOfIkTargets(TargetLockerType type, Transform toCopy)
    {
        SetWorldPositionRotationOfIkTarget(type, DummyHands.HandType.LeftHand, toCopy);
        SetWorldPositionRotationOfIkTarget(type, DummyHands.HandType.RightHand, toCopy);
    }

    public void SetWorldPositionRotationOfIkTarget(TargetLockerType type, DummyHands.HandType hand, Transform toCopy)
    {
        Transform targetToSet = ChooseTargetIk(type, hand);
        
        targetToSet.position = toCopy.position;
        targetToSet.rotation = toCopy.rotation;
    }

    public void SetParentTransformOfIkTarget(TargetLockerType type, DummyHands.HandType hand, Transform parent)
    {
        Transform targetToSet = ChooseTargetIk(type, hand);
        
        targetToSet.parent = parent;
        targetToSet.localPosition = Vector3.zero;
        targetToSet.localRotation = Quaternion.identity;
    }

    private Transform ChooseTargetIk(TargetLockerType type, DummyHands.HandType hand)
    {
        Transform targetToSet;
        
        switch (type)
        {
            case TargetLockerType.Pickup:
                targetToSet = hand == DummyHands.HandType.RightHand ? rightPickUpTargetLocker : leftPickUpTargetLocker;
                break;
            case TargetLockerType.Hold:
                targetToSet = hand == DummyHands.HandType.RightHand ? rightHoldTargetLocker : leftHoldTargetLocker;
                break;
            case TargetLockerType.ItemPosition:
                targetToSet = hand == DummyHands.HandType.RightHand ? rightItemPositionTargetLocker : leftItemPositionTargetLocker;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        return targetToSet;
    }

    public void UpdateItemHold(DummyItem item, bool alreadyInHand, DummyHand hand)
    {
         bool withTwoHands = SetConstraintsWeights(item, hand.handType, alreadyInHand);

        Transform hold = TargetFromHoldTypeAndHand(withTwoHands ? item.twoHandHold : item.singleHandHold, hand.handType);

        if (withTwoHands)
        {
            SetWorldPositionRotationOfIkTargets(TargetLockerType.ItemPosition, hold);
        }
        else if(hand.handType == DummyHands.HandType.LeftHand)
        {
            SetWorldPositionRotationOfIkTarget(TargetLockerType.ItemPosition,
                DummyHands.HandType.LeftHand, hold);
        }
        else
        {
            SetWorldPositionRotationOfIkTarget(TargetLockerType.ItemPosition,
                DummyHands.HandType.RightHand, hold);
        }
        

        SetOffsetOnItemPositionConstraint(withTwoHands ? item.twoHandHold : item.singleHandHold, hand.handType);
        
        MoveIkTargets(item, withTwoHands, hand);
        
        //if(alreadyInHand)
         //   StartCoroutine(MoveItemToHold(item.gameObject, 0.5f, hand));
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
        
        SetParentTransformOfIkTarget(TargetLockerType.Pickup,
            hand.handType,  primaryParent);
        SetParentTransformOfIkTarget(TargetLockerType.Hold,
            hand.handType,  primaryParent);

        if (withTwoHands)
        {
            SetParentTransformOfIkTarget(TargetLockerType.Pickup,
                hands.GetOtherHand(hand.handType).handType,  secondaryParent);
            SetParentTransformOfIkTarget(TargetLockerType.Hold,
                hands.GetOtherHand(hand.handType).handType,  secondaryParent);
        }

    }
}
