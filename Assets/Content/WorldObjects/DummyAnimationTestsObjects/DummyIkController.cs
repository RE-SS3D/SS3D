using UnityEngine;
using UnityEngine.Animations.Rigging;

public class DummyIkController : MonoBehaviour
{

    public DummyPickUp pickup;
    
    public DummyHands hands;

    // rig stuff
    
    public Rig pickUpRig;
    
    public Rig holdRig;
    

    
    [SerializeField]
    private MultiAimConstraint headIKConstraint;


    public void Start()
    {
        //pickup.OnHoldChange += HandleItemHoldChange;
    }

    private void HandleItemHoldChange(bool removeItem, DummyHand hand)
    {
        if (removeItem)
        {
            HandleRemoveItem(hand);
        }
        else
        {
            HandleAddItem(hand);
        }
    }

    private void HandleRemoveItem(DummyHand hand)
    {
        DummyHand otherHand = hands.GetOtherHand(hand.handType);
        if (otherHand.Empty)
        {
            otherHand.pickupIkConstraint.weight = 0f;
            otherHand.holdIkConstraint.weight = 0f;
        }

        hand.pickupIkConstraint.weight = 0f;
        hand.holdIkConstraint.weight = 0f;

        if (otherHand.Full && otherHand.item.canHoldTwoHand)
        {
            hand.pickupIkConstraint.weight = 1f;
            hand.holdIkConstraint.weight = 1f;
        }
    }

    private void HandleAddItem(DummyHand hand)
    {
        DummyHand otherHand = hands.GetOtherHand(hand.handType);
        
        if (hand.item.canHoldTwoHand && otherHand.Empty)
        {
            otherHand.pickupIkConstraint.weight = 1f;
            otherHand.holdIkConstraint.weight = 1f;
        }
        
        hand.pickupIkConstraint.weight = 1f;
        hand.holdIkConstraint.weight = 1f;
    }
}
