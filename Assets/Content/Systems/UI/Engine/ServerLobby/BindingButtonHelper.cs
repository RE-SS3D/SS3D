using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

namespace SS3D.Engine.Input
{
    /// <summary>
    /// Behavior responsible for handling rebinding an action when the attached button is pressed.
    /// Should be attached to a button with a TMP_Text on it.
    /// Used in "Content/Systems/UI/Engine/ServerLobby/BindingButton.prefab".
    /// </summary>
    public class BindingButtonHelper : MonoBehaviour
    {
        private bool isRebinding = false;
        private RebindingOperation rebinding;

        public InputAction inputActionToBind;
        public TMP_Text buttonText;
        public bool isRebindable;

        public void OnClicked()
        {
            if (isRebindable)
            {
                Debug.Log("Rebinding " + inputActionToBind.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions));
                // Sanity check, although not really necessary because all actions are disabled once pressed.
                if (isRebinding)
                {
                    return;
                }
                isRebinding = true;
                InputHelper.Inputs.Disable();
                buttonText.text = "Rebinding";
                rebinding = inputActionToBind.PerformInteractiveRebinding()
                    .OnComplete(EndRebind)
                    .OnCancel(EndRebind)
                    .Start();
            }
            else
            {
                buttonText.text = "Cannot Rebind!";
                Invoke("ResetText", 2);
            }
        }

        private void ResetText()
        {
            if (!isRebinding)
            {
                buttonText.text = inputActionToBind.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
            }
        }

        /// <summary>
        /// Cleans up the rebind operation, sets the button to the new input and saves the new input binding.
        /// </summary>
        private void EndRebind(RebindingOperation bind)
        {
            rebinding.Dispose();
            InputHelper.Inputs.Enable();
            PlayerPrefs.SetString("bindings", InputHelper.Inputs.SaveBindingOverridesAsJson());
            isRebinding = false;
            ResetText();
        }
    }
}
