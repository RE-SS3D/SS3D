using System;
using UnityEngine;

public class DummyHands : MonoBehaviour
{

    public DummyHand leftHand;
    public DummyHand rightHand;

    public HandType selectedHand = HandType.LeftHand;
    
    public DummyHand SelectedHand => selectedHand == HandType.LeftHand ? leftHand : rightHand;
    
    public DummyHand UnselectedHand => selectedHand == HandType.LeftHand ? rightHand : leftHand;

    public DummyHand GetHand(HandType hand) => hand == HandType.LeftHand ? leftHand : rightHand;
    
    public DummyHand GetOtherHand(HandType hand) => hand == HandType.LeftHand ? rightHand : leftHand;
    
    public bool BothHandFull => leftHand.Full && rightHand.Full;
    public bool BothHandEmpty => leftHand.Empty && rightHand.Empty;
    
    public event EventHandler<DummyHand> OnSelectedHandChange;


    // Update is called once per frame
    public void Update()
    {
        if (!Input.GetKeyDown(KeyCode.X))
            return;
        
       

        selectedHand = selectedHand == HandType.LeftHand ? HandType.RightHand : HandType.LeftHand;
        
        OnSelectedHandChange?.Invoke(this, SelectedHand );
        
        Debug.Log($"Selected hand is {selectedHand}");
    }
}
