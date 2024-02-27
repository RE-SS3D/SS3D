using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class DummyIkController : MonoBehaviour
{

    public GameObject RightHandIkTarget;

    [SerializeField]
    private TwoBoneIKConstraint _rightArmTwoBoneIKConstraint;
    

    
    [SerializeField]
    private ChainIKConstraint _rightArmChainIKConstraint;
    
    [SerializeField]
    private MultiAimConstraint _headIKConstraint;
    
    public ChainIKConstraint  RightArmChainIKConstraint => _rightArmChainIKConstraint;

    public TwoBoneIKConstraint RightArmTwoBoneIKConstraint => _rightArmTwoBoneIKConstraint;
    
    public MultiAimConstraint HeadIKConstraint => _headIKConstraint;
}
