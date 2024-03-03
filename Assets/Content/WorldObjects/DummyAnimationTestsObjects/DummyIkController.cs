using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public class DummyIkController : MonoBehaviour
{

    public enum IkTargetType
    {
        Pickup,
        Hold,
        ItemPosition,
    }

    // The transforms that moves to put itself on hold positions
    public Transform rightHandPickUpIkTarget;
    
    public Transform leftHandPickUpIkTarget;
    
    public Transform rightHandHoldIkTarget;

    public Transform leftHandHoldIkTarget;

    public Transform rightHandItemPositionIkTarget;

    public Transform leftHandItemPositionIkTarget;

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

    public Transform SelectedHandHoldIkTarget => 
        GetComponent<DummyHands>().selectedHand == DummyHands.Hand.RightHand ? rightHandHoldIkTarget : leftHandHoldIkTarget;

    public Transform SelectedHandPickupIkTarget => 
        GetComponent<DummyHands>().selectedHand == DummyHands.Hand.RightHand ? rightHandPickUpIkTarget : leftHandPickUpIkTarget;

    public Transform SelectedHandItemPositionIkTarget => 
        GetComponent<DummyHands>().selectedHand == DummyHands.Hand.RightHand ? rightHandItemPositionIkTarget : leftHandItemPositionIkTarget;
    
    public void Start()
    {
        holdData.Add(new(DummyItem.HandHoldType.DoubleHandGun, gunHoldLeft,
            new Vector3(0.15f,-0.08f,0.26f), DummyHands.Hand.LeftHand));
        holdData.Add(new(DummyItem.HandHoldType.DoubleHandGun, gunHoldRight,
            new Vector3(-0.15f,-0.08f,0.26f), DummyHands.Hand.RightHand));
        holdData.Add(new(DummyItem.HandHoldType.Toolbox, toolBoxHoldLeft,
            new Vector3(0.06f,-0.64f,0.11f), DummyHands.Hand.LeftHand));
        holdData.Add(new(DummyItem.HandHoldType.Toolbox, toolboxHoldRight,
            new Vector3(-0.06f, -0.64f, 0.11f), DummyHands.Hand.RightHand));
        holdData.Add(new(DummyItem.HandHoldType.Shoulder, shoulderHoldLeft,
            new Vector3(0f, 0.18f, 0f), DummyHands.Hand.LeftHand));
        holdData.Add(new(DummyItem.HandHoldType.Shoulder, shoulderHoldRight,
            new Vector3(0f, 0.18f, 0f), DummyHands.Hand.RightHand));
    }
    
    public record HoldAndOffset(DummyItem.HandHoldType HandHoldType, Transform holdTarget, Vector3 Offset, DummyHands.Hand PrimaryHand);

    public Transform TargetFromHoldTypeAndHand(DummyItem.HandHoldType handHoldType, DummyHands.Hand selectedHand)
    {
        return holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).holdTarget;
    }

    private Vector3 OffsetFromHoldTypeAndHand(DummyItem.HandHoldType handHoldType, DummyHands.Hand selectedHand)
    {
        return holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).Offset;
    }

    public void SetOffsetOnItemPositionConstraint(DummyItem.HandHoldType holdType, DummyHands.Hand selectedHand)
    {
        itemLeftHoldPositionIkConstraint.data.offset = OffsetFromHoldTypeAndHand(holdType, selectedHand);
        itemRightHoldPositionIkConstraint.data.offset = OffsetFromHoldTypeAndHand(holdType, selectedHand);
    }
    
    public void SetWorldPositionRotationOfIkTargets(IkTargetType type, Transform toCopy)
    {
        SetWorldPositionRotationOfIkTarget(type, DummyHands.Hand.LeftHand, toCopy);
        SetWorldPositionRotationOfIkTarget(type, DummyHands.Hand.RightHand, toCopy);
    }

    public void SetWorldPositionRotationOfIkTarget(IkTargetType type, DummyHands.Hand hand, Transform toCopy)
    {
        Transform targetToSet = ChooseTargetIk(type, hand);
        
        targetToSet.position = toCopy.position;
        targetToSet.rotation = toCopy.rotation;
    }

    public void SetParentTransformOfIkTarget(IkTargetType type, DummyHands.Hand hand, Transform parent)
    {
        Transform targetToSet = ChooseTargetIk(type, hand);
        
        targetToSet.parent = parent;
        targetToSet.localPosition = Vector3.zero;
        targetToSet.localRotation = Quaternion.identity;
    }

    private Transform ChooseTargetIk(IkTargetType type, DummyHands.Hand hand)
    {
        Transform targetToSet;
        
        switch (type)
        {
            case IkTargetType.Pickup:
                targetToSet = hand == DummyHands.Hand.RightHand ? rightHandPickUpIkTarget : leftHandPickUpIkTarget;
                break;
            case IkTargetType.Hold:
                targetToSet = hand == DummyHands.Hand.RightHand ? rightHandHoldIkTarget : leftHandHoldIkTarget;
                break;
            case IkTargetType.ItemPosition:
                targetToSet = hand == DummyHands.Hand.RightHand ? rightHandItemPositionIkTarget : leftHandItemPositionIkTarget;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        return targetToSet;
    }
    
    
    
}
