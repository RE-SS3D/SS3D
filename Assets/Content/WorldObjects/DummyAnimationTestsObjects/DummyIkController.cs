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

    public Transform rightUpperArm;

    public Transform leftUpperArm;
    

    // rig stuff
    public Rig pickUpRig;
    
    public Rig holdRig;
    
    public TwoBoneIKConstraint rightHandHoldTwoBoneIkConstraint;
    
    public TwoBoneIKConstraint leftHandHoldTwoBoneIkConstraint;

    public MultiPositionConstraint itemRightHoldPositionIkConstraint;
    
    public MultiPositionConstraint itemLeftHoldPositionIkConstraint;

    // Hold positions
    
    // The game object that moves to put itself on holds
    public Transform rightHoldPositionTarget;
    
    public Transform leftHoldPositionTarget;
    
    public Transform gunHoldRight;

    public Transform gunHoldLeft;

    public Transform toolboxHoldRight;

    public Transform toolBoxHoldLeft;

    public Transform shoulderHoldRight;

    public Transform shoulderHoldLeft;

    

    public ChainIKConstraint rightArmChainIKConstraint;
    
    public ChainIKConstraint leftArmChainIKConstraint;
    
    [SerializeField]
    private MultiAimConstraint _headIKConstraint;
    
    

    
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

    private Vector3 OffsetFromHoldTypeAndHand(DummyItem.SingleHandHoldType singleHoldType, DummyItem.TwoHandHoldType twoHandHoldType,
        bool withTwoHand, DummyHands.Hand selectedHand)
    {
        if (withTwoHand)
        {
            switch (twoHandHoldType)
            {
                case DummyItem.TwoHandHoldType.Gun :
                    return selectedHand == DummyHands.Hand.LeftHand ? new Vector3(0.15f,-0.08f,0.26f) : new Vector3(-0.15f,-0.08f,0.26f);
            }
        }
        else
        {
            switch (singleHoldType)
            {
                case DummyItem.SingleHandHoldType.Shoulder :
                    return selectedHand == DummyHands.Hand.LeftHand ? new Vector3()  : new Vector3();
                case  DummyItem.SingleHandHoldType.Toolbox :
                    return selectedHand == 
                        DummyHands.Hand.LeftHand ? new Vector3(0.06f,-0.64f,0.11f)  : new Vector3(-0.06f, -0.64f, 0.11f);
            }
        }
        
        // if no match return a toolbox hold.
        return selectedHand == DummyHands.Hand.LeftHand ? new Vector3(0.06f,-0.64f,0.11f)  : new Vector3(-0.06f, -0.64f, 0.11f);
    }

    public void SetOffsetOnItemPositionConstraint(DummyItem.SingleHandHoldType singleHoldType, DummyItem.TwoHandHoldType twoHandHoldType,
        bool withTwoHand, DummyHands.Hand selectedHand, Transform hold, bool onRightShoulder)
    {
        itemLeftHoldPositionIkConstraint.data.offset = OffsetFromHoldTypeAndHand(singleHoldType, twoHandHoldType, withTwoHand, selectedHand);
        itemRightHoldPositionIkConstraint.data.offset = OffsetFromHoldTypeAndHand(singleHoldType, twoHandHoldType, withTwoHand, selectedHand);
    }
    
    /// left hint toolbox pose (0.148, -0.549, 0.338)
    /// target hold rotation 0 270 0
    /// Offset Vector3(0.0599999987,-0.639999986,0.109999999)
    /// left hold position Vector3(-0.0149999997,0.379999995,0.238999993)
    /// left hold rotation (348.4, 0, 180)
}
