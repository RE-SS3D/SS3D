using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class DummyIkController : MonoBehaviour
{
    [SerializeField]
    private GameObject _rightPunchTarget;

    [SerializeField]
    private TwoBoneIKConstraint _rightArmTwoBoneIKConstraint;
    
    public GameObject  RightPunchTarget => _rightPunchTarget;
    
    [SerializeField]
    private ChainIKConstraint _rightArmChainIKConstraint;
    
    [SerializeField]
    private MultiAimConstraint _headIKConstraint;
    
    public ChainIKConstraint  RightArmChainIKConstraint => _rightArmChainIKConstraint;

    public TwoBoneIKConstraint RightArmTwoBoneIKConstraint => _rightArmTwoBoneIKConstraint;
    
    public MultiAimConstraint HeadIKConstraint => _headIKConstraint;
}
