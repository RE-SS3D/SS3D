using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class DummyIkController : MonoBehaviour
{

    public GameObject RightHandIkTarget;

    public GameObject RightUpperArm;

    public Rig pickUpRig;
    
    public Rig holdRig;

    public GameObject gunHold;

    
    [SerializeField]
    private ChainIKConstraint _rightArmChainIKConstraint;
    
    [SerializeField]
    private MultiAimConstraint _headIKConstraint;
    
    public ChainIKConstraint  RightArmChainIKConstraint => _rightArmChainIKConstraint;
    
    public MultiAimConstraint HeadIKConstraint => _headIKConstraint;
}
