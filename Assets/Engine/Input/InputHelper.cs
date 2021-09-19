using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Engine.Input {

    /// <summary>
    /// A static class that holds the current actions and their bindings.
    /// Can be referenced from anywhere.
    /// </summary>
    public class InputHelper : MonoBehaviour
    {

        public static InputActions Inputs { get; } = new InputActions();

        /// <summary>
        /// Loads and enables the user's saved bindings, if they have any.
        /// </summary>
        static InputHelper()
        {
            string bindings = PlayerPrefs.GetString("bindings", null);
            if (bindings != null)
            {
                try
                {
                    Inputs.LoadBindingOverridesFromJson(bindings);
                }
                catch (Exception e)
                {
                    Debug.Log(e.StackTrace);
                }
            }
            Inputs.Enable();
        }
    }
}
