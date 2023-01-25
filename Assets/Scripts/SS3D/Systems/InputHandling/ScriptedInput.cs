using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.InputHandling
{
    /// <summary>
    /// ScriptedInput allows for the input to be directly scripted by the developer. The
    /// primary use case for this class is for automated testing so that test dummies can
    /// be appropriately puppeteered.
    /// </summary>
    public class ScriptedInput : IInputService
    {
        private Dictionary<string, float> axisRaw;
        private Dictionary<string, bool> button;
        private Dictionary<string, bool> buttonUp;
        private Dictionary<string, bool> buttonDown;

        /// <summary>
        /// Gets a float value corresponding to a defined input axis.
        /// </summary>
        /// <param name="axisName">The input key you are seeking</param>
        /// <returns>The value corresponding to the input key</returns>
        public float GetAxisRaw(string axisName)
        {
            float result = 0f;
            if (axisRaw == null)
            {
                return result;
            }
            else
            {
                if (axisRaw.TryGetValue(axisName, out result))
                {
                    return result;
                }
                else
                {
                    return 0f;
                }
            }
        }

        /// <summary>
        /// Sets the value to be returned from the GetAxisRaw method.
        /// </summary>
        /// <param name="axisName">The key corresponding to the input system axis name</param>
        /// <param name="value">The value you want returned from GetAxisRaw</param>
        public void SetAxisRaw(string axisName, float value)
        {
            // Create dictionary if required
            if (axisRaw == null) axisRaw = new Dictionary<string, float>();

            // Add the entry into the dictionary
            if (!axisRaw.ContainsKey(axisName))
            {
                axisRaw.Add(axisName, value);
            }
            else
            {
                axisRaw[axisName] = value;
            }
        }

        /// <summary>
        /// Simulates the user either pressing or releasing a button.
        /// </summary>
        /// <param name="buttonName">The button to handle</param>
        /// <param name="pushDown">True to simulate pushing, false to simulate releasing</param>
        public void HandleButton(string buttonName, bool pushDown)
        {
            // Create dictionaries if required.
            if (button == null) InitializeButtonDictionaries();

            // Exit early if the button is already in the correct state.
            if (GetButton(buttonName) == pushDown) return;

            // Set the entries in the dictionaries
            if (!button.ContainsKey(buttonName))
            {
                SetDictionariesInitial(buttonName, pushDown);
            }
            else
            {
                SetDictionariesSubsequent(buttonName, pushDown);
            }

        }


        private void SetDictionariesInitial(string buttonName, bool buttonDepressed)
        {
            button.Add(buttonName, buttonDepressed);
            buttonDown.Add(buttonName, buttonDepressed);
            buttonUp.Add(buttonName, !buttonDepressed);
        }

        private void SetDictionariesSubsequent(string buttonName, bool buttonDepressed)
        {
            button[buttonName] = buttonDepressed;
            buttonDown[buttonName] = buttonDepressed;
            buttonUp[buttonName] = !buttonDepressed;
        }

        private void InitializeButtonDictionaries()
        {
            button = new Dictionary<string, bool>();
            buttonUp = new Dictionary<string, bool>();
            buttonDown = new Dictionary<string, bool>();
        }

        /// <summary>
        /// Checks whether a particular button was is currently held down.
        /// </summary>
        /// <param name="buttonName">The button to check</param>
        /// <returns>Whether the button is currently held down</returns>
        public bool GetButton(string buttonName)
        {
            return GetResultFromDictionary(buttonName, buttonDown);

        }

        /// <summary>
        /// Checks whether a particular button was pressed down this frame.
        /// </summary>
        /// <param name="buttonName">The button to check</param>
        /// <returns>Whether the button was pressed down this frame</returns>
        public bool GetButtonDown(string buttonName)
        {
            return GetResultFromDictionary(buttonName, buttonDown, true);
        }

        /// <summary>
        /// Checks whether a particular button was released this frame.
        /// </summary>
        /// <param name="buttonName">The button to check</param>
        /// <returns>Whether the button was released this frame</returns>
        public bool GetButtonUp(string buttonName)
        {
            return GetResultFromDictionary(buttonName, buttonUp, true);
        }

        private bool GetResultFromDictionary(string buttonName, Dictionary<string, bool> dictionary, bool overwriteAfterRead = false)
        {
            // Initialize an empty result
            bool result = false;

            // Exit early if the dictionary has not yet been created
            if (dictionary == null)
            {
                return result;
            }

            // Attempt to get the value at the key
            if (dictionary.TryGetValue(buttonName, out result))
            {
                // Entry exists for that key. Overwrite it if required.
                if (overwriteAfterRead)
                {
                    dictionary[buttonName] = false;
                }
                
                // Result will be returned.
                return result;
            }
            else
            {
                // No entry listed for that key.
                return false;
            }
        }
    }
}