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

    public Transform ItemPositionIkTarget(DummyHands.Hand hand)
    {
        return hand == DummyHands.Hand.RightHand ? rightHandItemPositionIkTarget : leftHandItemPositionIkTarget;
    }

    public Transform PickUpTargetLocker(DummyHands.Hand hand)
    {
        return hand == DummyHands.Hand.RightHand ? rightHandPickUpIkTarget : leftHandPickUpIkTarget;
    }

    public Transform UpperArm(DummyHands.Hand hand)
    {
        return hand == DummyHands.Hand.RightHand ? rightUpperArm : leftUpperArm;
    }
    
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
        if(selectedHand == DummyHands.Hand.RightHand)
            itemRightHoldPositionIkConstraint.data.offset = OffsetFromHoldTypeAndHand(holdType, selectedHand);
        else
            itemLeftHoldPositionIkConstraint.data.offset = OffsetFromHoldTypeAndHand(holdType, selectedHand);
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

    public void UpdateItemHold(DummyItem item, bool alreadyInHand, DummyHands.Hand hand)
    {
         bool withTwoHands = false;

        if ((hands.BothHandEmpty || (!hands.OtherHandFull(hand) && alreadyInHand))  && item.canHoldTwoHand)
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
            
            if (hand == DummyHands.Hand.LeftHand)
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
        else
        {
            return;
        }

        Transform hold = TargetFromHoldTypeAndHand(withTwoHands ? item.twoHandHold : item.singleHandHold, hand);

        if (withTwoHands)
        {
            SetWorldPositionRotationOfIkTargets(IkTargetType.ItemPosition, hold);
        }
        else if(hand == DummyHands.Hand.LeftHand)
        {
            SetWorldPositionRotationOfIkTarget(IkTargetType.ItemPosition,
                DummyHands.Hand.LeftHand, hold);
        }
        else
        {
            SetWorldPositionRotationOfIkTarget(IkTargetType.ItemPosition,
                DummyHands.Hand.RightHand, hold);
        }
        

        SetOffsetOnItemPositionConstraint(withTwoHands ? item.twoHandHold : item.singleHandHold, hand);
        
        MoveIkTargets(item, withTwoHands, hand);
        
        if(alreadyInHand)
            StartCoroutine(MoveItemToHold(item.gameObject, 0.5f, hand));
    }
    
    private void MoveIkTargets(DummyItem item, bool withTwoHands, DummyHands.Hand hand)
    {
        Transform primaryParent = hand == DummyHands.Hand.RightHand ?
            item.primaryRightHandHold.transform : item.primaryLeftHandHold.transform;
        
        Transform secondaryParent = hand == DummyHands.Hand.RightHand ?
            item.secondaryLeftHandHold.transform : item.secondaryRightHandHold.transform;
        
        SetParentTransformOfIkTarget(IkTargetType.Pickup,
            hand,  primaryParent);
        SetParentTransformOfIkTarget(IkTargetType.Hold,
            hand,  primaryParent);

        if (withTwoHands)
        {
            SetParentTransformOfIkTarget(IkTargetType.Pickup,
                hands.OtherHand(hand),  secondaryParent);
            SetParentTransformOfIkTarget(IkTargetType.Hold,
                hands.OtherHand(hand),  secondaryParent);
        }

    }
    
    public IEnumerator MoveItemToHold(GameObject item, float itemMoveDuration, DummyHands.Hand hand)
    {
        Vector3 initialPosition = item.transform.position;
        Quaternion initialRotation = item.transform.rotation;
        float timer = 0.0f;
        Transform targetHold = ItemPositionIkTarget(hand);

        while (timer < itemMoveDuration)
        {
            float t = timer / itemMoveDuration;
            item.transform.position = Vector3.Lerp(initialPosition, targetHold.position, t);
            item.transform.rotation = Quaternion.Lerp(initialRotation, targetHold.rotation, t);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final transform matches the target values exactly
        item.transform.position = targetHold.position;
        item.transform.rotation = targetHold.rotation;
        item.transform.parent = targetHold;
    }
    
    
    
}
