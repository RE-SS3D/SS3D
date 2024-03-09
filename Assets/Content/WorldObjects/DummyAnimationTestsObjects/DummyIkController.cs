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

    public DummyPickUp pickup;
    
    public DummyHands hands;

    public IntentController intents;

    // rig stuff
    
    public Rig pickUpRig;
    
    public Rig holdRig;
    
    public TwoBoneIKConstraint rightHandHoldTwoBoneIkConstraint;
    
    public TwoBoneIKConstraint leftHandHoldTwoBoneIkConstraint;
    
    public ChainIKConstraint rightArmChainIKConstraint;
    
    public ChainIKConstraint leftArmChainIKConstraint;
    
    public MultiAimConstraint headIKConstraint;


    public void Start()
    {
        pickup.OnHoldChange += HandleItemHoldChange;
    }
    
    private void HandleItemHoldChange(bool removeItem)
    {
        if(removeItem) return;
        bool withTwoHands = SetConstraintsWeights(hands.SelectedHand, true);
        MoveIkTargets(withTwoHands, hands.SelectedHand);
    }


    private bool SetConstraintsWeights(DummyHand hand, bool alreadyInHand)
    {
        DummyItem item = hand.itemInHand;
        bool withTwoHands = hands.WithTwoHands(item, hand.handType, alreadyInHand);
        
        if (withTwoHands)
        {
            rightArmChainIKConstraint.weight = 1;
            leftArmChainIKConstraint.weight = 1;
            rightHandHoldTwoBoneIkConstraint.weight = 1;
            leftHandHoldTwoBoneIkConstraint.weight = 1;
            item.heldWithTwoHands = true;
        }
        else if (item.canHoldOneHand)
        {
            item.heldWithOneHand = true;
            
            if (hand.handType == DummyHands.HandType.LeftHand)
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
    
    private void MoveIkTargets(bool withTwoHands, DummyHand hand)
    {
        DummyItem item = hand.itemInHand;
        
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
