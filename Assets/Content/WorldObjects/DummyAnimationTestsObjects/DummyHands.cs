using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyHands : MonoBehaviour
{

    public DummyHand leftHand;
    public DummyHand rightHand;
    
    public enum HandType
    {
        LeftHand = 0,
        RightHand = 1,
    }

    public HandType selectedHand = HandType.LeftHand;
    
    public DummyHand SelectedHand => selectedHand == HandType.LeftHand ? leftHand : rightHand;
    
    public DummyHand UnselectedHand => selectedHand == HandType.LeftHand ? rightHand : leftHand;

    public DummyHand GetHand(HandType hand) => hand == HandType.LeftHand ? leftHand : rightHand;
    
    public DummyHand GetOtherHand(HandType hand) => hand == HandType.LeftHand ? rightHand : leftHand;
    
    public bool BothHandFull => leftHand.Full && rightHand.Full;
    public bool BothHandEmpty => leftHand.Empty && rightHand.Empty;


    // Update is called once per frame
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.X))
            return;

        selectedHand = selectedHand == HandType.LeftHand ? HandType.RightHand : HandType.LeftHand;
        
        Debug.Log($"Selected hand is {selectedHand}");
    }
}
