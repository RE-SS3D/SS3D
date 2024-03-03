using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyItem : MonoBehaviour
{

    public enum HandHoldType
    {
        None,
        Toolbox,
        Shoulder,
        DoubleHandGun,
    }

    public HandHoldType singleHandHold;
    
    public HandHoldType twoHandHold;

    
    public Transform primaryRightHandHold;
    
    public Transform secondaryRightHandHold;
    
    public Transform primaryLeftHandHold;

    public Transform secondaryLeftHandHold;

    public bool canHoldTwoHand;

    public bool canHoldOneHand;
    
}
