using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public class DummyIkController : MonoBehaviour
{

    public GameObject rightHandPickUpIkTarget;
    
    public GameObject leftHandPickUpIkTarget;
    
    public GameObject rightHandHoldIkTarget;
    
    public GameObject leftHandHoldIkTarget;

    public GameObject holdWithNoRootRotation;

    public Transform rightUpperArm;

    public Transform leftUpperArm;
    

    // rig stuff
    public Rig pickUpRig;
    
    public Rig holdRig;
    
    public TwoBoneIKConstraint rightHandHoldTwoBoneIkConstraint;
    
    public TwoBoneIKConstraint leftHandHoldTwoBoneIkConstraint;

    public MultiPositionConstraint itemHoldPositionIkConstraint;

    // Hold positions
    
    // The game object that moves to put itself on holds
    public Transform holdPositionTarget;
    
    public Transform gunHoldRight;

    public Transform gunHoldLeft;

    public Transform toolboxHoldRight;

    public Transform toolBoxHoldLeft;

    public Transform shoulderHoldRight;

    public Transform shoulderHoldLeft;

    
    [SerializeField]
    private ChainIKConstraint _rightArmChainIKConstraint;
    
    [SerializeField]
    private MultiAimConstraint _headIKConstraint;
    
    
    
    public ChainIKConstraint  RightArmChainIKConstraint => _rightArmChainIKConstraint;
    
    public MultiAimConstraint HeadIKConstraint => _headIKConstraint;

    public Transform TargetFromHoldTypeAndHand(DummyItem.SingleHandHoldType singleHoldType, DummyItem.TwoHandHoldType twoHandHoldType,
        bool withTwoHand, DummyHands.Hand selectedHand)
    {
        if (withTwoHand)
        {
            switch (twoHandHoldType)
            {
                case DummyItem.TwoHandHoldType.Gun :
                    return selectedHand == DummyHands.Hand.LeftHand ? gunHoldLeft : gunHoldRight;
            }
        }
        else
        {
            switch (singleHoldType)
            {
                case DummyItem.SingleHandHoldType.Shoulder :
                    return selectedHand == DummyHands.Hand.LeftHand ? shoulderHoldLeft  : shoulderHoldRight;
                case  DummyItem.SingleHandHoldType.Toolbox :
                    return selectedHand == DummyHands.Hand.LeftHand ? toolBoxHoldLeft  : toolboxHoldRight;
            }
        }
        
        // if no match return a toolbox hold.
        return selectedHand == DummyHands.Hand.LeftHand ? toolBoxHoldLeft  : toolboxHoldRight;
    }

    public void SetOffsetOnItemPositionConstraint(Transform hold, bool onRightShoulder)
    {
        Transform arm = onRightShoulder ? rightUpperArm : leftUpperArm;
        
        Debug.Log(arm.position);
        
        // probably doesn't work because of rotations
        Vector3 holdPositionRelative = arm.InverseTransformPoint(hold.position);
        
        var sources = itemHoldPositionIkConstraint.data.sourceObjects;
        sources.SetWeight(0, onRightShoulder ? 1 : 0);
        sources.SetWeight(1, onRightShoulder ? 0 : 1);
        itemHoldPositionIkConstraint.data.sourceObjects = sources;

       
       itemHoldPositionIkConstraint.data.offset = holdPositionRelative-arm.localPosition;
    }
}
