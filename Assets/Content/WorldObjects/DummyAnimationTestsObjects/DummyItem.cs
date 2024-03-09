using System;
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

    public HandHoldType GetHoldType(bool withTwoHands, Intent intent)
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

    public Transform GetHold(bool primary, HandType handType)
    {
        switch (primary, handType)
        {
            case (true, HandType.LeftHand):
                return primaryLeftHandHold;
            case (false, HandType.LeftHand):
                return secondaryLeftHandHold;
            case (true, HandType.RightHand):
                return primaryRightHandHold;
            case (false, HandType.RightHand):
                return secondaryRightHandHold;
            default:
                throw new ArgumentException();
        }
    }

}
