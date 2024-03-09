using UnityEngine;
using UnityEngine.Animations.Rigging;

public class DummyIkController : MonoBehaviour
{

    public DummyPickUp pickup;
    
    public DummyHands hands;

    public IntentController intents;

    // rig stuff
    
    public Rig pickUpRig;
    
    public Rig holdRig;
    
    public TwoBoneIKConstraint rightHandHoldTwoBoneIkConstraint;
    
    public TwoBoneIKConstraint leftHandHoldTwoBoneIkConstraint;
    
    public ChainIKConstraint rightArmChainIKConstraint;
    
    public ChainIKConstraint leftArmChainIKConstraint;
    
    public MultiAimConstraint headIKConstraint;


    private ChainIKConstraint SelectedArmChainIKConstraint =>
        hands.SelectedHand.handType == HandType.LeftHand ? leftArmChainIKConstraint : rightArmChainIKConstraint;
    
    private ChainIKConstraint UnselectedArmChainIKConstraint =>
        hands.SelectedHand.handType == HandType.LeftHand ? rightArmChainIKConstraint : leftArmChainIKConstraint;
    
    private TwoBoneIKConstraint SelectedHandHoldTwoBoneIkConstraint =>
        hands.SelectedHand.handType == HandType.LeftHand ? leftHandHoldTwoBoneIkConstraint : rightHandHoldTwoBoneIkConstraint;
    
    private TwoBoneIKConstraint UnselectedHandHoldTwoBoneIkConstraint =>
        hands.SelectedHand.handType == HandType.LeftHand ? rightHandHoldTwoBoneIkConstraint : leftHandHoldTwoBoneIkConstraint;

    public void Start()
    {
        pickup.OnHoldChange += HandleItemHoldChange;
    }
    
    private void HandleItemHoldChange(bool removeItem)
    {
        if (removeItem)
        {
            if (hands.UnselectedHand.Empty)
            {
                UnselectedArmChainIKConstraint.weight = 0f;
                UnselectedHandHoldTwoBoneIkConstraint.weight = 0f;
            }

            SelectedArmChainIKConstraint.weight = 0f;
            SelectedHandHoldTwoBoneIkConstraint.weight = 0f;

            if (hands.UnselectedHand.Full && hands.UnselectedHand.item.canHoldTwoHand)
            {
                SelectedArmChainIKConstraint.weight = 1f;
                SelectedHandHoldTwoBoneIkConstraint.weight = 1f;
            }

        }
        else
        {
            if (hands.SelectedHand.item.canHoldTwoHand && hands.UnselectedHand.Empty)
            {
                UnselectedArmChainIKConstraint.weight = 1f;
                UnselectedHandHoldTwoBoneIkConstraint.weight = 1f;
            }
            
            SelectedArmChainIKConstraint.weight = 1f;
            SelectedHandHoldTwoBoneIkConstraint.weight = 1f;
        }
    }
}
