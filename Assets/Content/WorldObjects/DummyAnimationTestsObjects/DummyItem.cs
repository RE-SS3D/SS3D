using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyItem : MonoBehaviour
{

    public enum SingleHandHoldType
    {
        Toolbox,
        Shoulder,
    }

    public enum TwoHandHoldType
    {
        Gun,
    }

    public SingleHandHoldType singleHandHold;

    public TwoHandHoldType twoHandHold;
    
    public Transform primaryRightHandHold;
    
    public Transform secondaryRightHandHold;
    
    public Transform primaryLeftHandHold;

    public Transform secondaryLeftHandHold;

    public bool canHoldTwoHand;

    public bool canHoldOneHand;
    
    // Start is called before the first frame update
    private void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        
    }
}
