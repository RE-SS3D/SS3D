using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using SS3D.Engine.Input;
using TMPro;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

public class BindingButtonHelper : MonoBehaviour
{
    private bool isRebinding = false;
    private RebindingOperation rebinding;

    public InputAction inputActionToBind;
    public TMP_Text text;

    public void OnClicked()
    {
        Debug.Log("Clicked");
        if (isRebinding)
            return;
        isRebinding = true;
        inputActionToBind.Disable();
        rebinding = inputActionToBind.PerformInteractiveRebinding()
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(bind => {
                rebinding.Dispose();
                isRebinding = false;
                text.text = inputActionToBind.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
                inputActionToBind.Enable();
            })
            .Start();
    }
}
