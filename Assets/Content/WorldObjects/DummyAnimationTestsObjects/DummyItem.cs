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
    
    public Transform rightHandHold;
    
    public Transform leftHandHold;

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
