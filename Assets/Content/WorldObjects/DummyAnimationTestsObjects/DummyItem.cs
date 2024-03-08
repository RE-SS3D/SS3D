using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyItem : MonoBehaviour
{
    public HandHoldType singleHandHold;
    
    public HandHoldType twoHandHold;

    public HandHoldType singleHandHoldHarm;

    public HandHoldType twoHandHoldHarm;
    
    public Transform primaryRightHandHold;
    
    public Transform secondaryRightHandHold;
    
    public Transform primaryLeftHandHold;

    public Transform secondaryLeftHandHold;

    public bool canHoldTwoHand;

    public bool canHoldOneHand;

    public bool heldWithOneHand;

    public bool heldWithTwoHands;

    public HandHoldType GetHold(bool withTwoHands, Intent intent)
    {
        switch (intent, withTwoHands)
        {
            case (Intent.Def, true):
                return twoHandHold;
            case (Intent.Def, false):
                return singleHandHold;
            case (Intent.Harm, true):
                return twoHandHoldHarm;
            case (Intent.Harm, false):
                return singleHandHoldHarm;
        }
        
        Debug.LogError("case not handled");

        return singleHandHold;
    }

}
