using NUnit.Framework;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.InputHandling;
using SS3D.UI.Buttons;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Tests
{
    /// <summary>
    /// This class is simply a container for helper methods for UnityTests.
    /// </summary>
    public static class TestHelpers
    {
        #region Constants
        private const string HorizontalAxis = "Horizontal";
        private const string VerticalAxis = "Vertical";
        private const string CancelButton = "Cancel";
        private const string ReadyButtonName = "Ready";
        private const string EmbarkButtonName = "Embark";

        private const string ServerSettingsTabName = "Server Settings";
        private const string StartRoundButtonName = "Start Round";
        #endregion

        public static IEnumerator Move(string axis, float value, float duration = 1f)
        {
            yield return Move(new[] { axis }, new[] { value }, duration);
        }

        public static IEnumerator Move(string[] axis, float[] value, float duration = 1f)
        {
            ScriptedInput input = UserInput.GetInputService() as ScriptedInput; 

            // Initial minor delay to enforce separation of commands
            yield return new WaitForSeconds(0.25f);

            // Start holding down the appropriate keys.
            for (int i = 0; i < axis.Length; i++)
            {
                input.SetAxisRaw(axis[i], value[i]);
            }

            // Wait for a little, then release the key.
            yield return new WaitForSeconds(duration);

            // Release the keys.
            for (int i = 0; i < axis.Length; i++)
            {
                input.SetAxisRaw(axis[i], 0);
            }

            // Wait for a little more, to add clear separation from the next move command.
            yield return new WaitForSeconds(0.25f);
        }

        /// <summary>
        /// Checks whether two floats are approximately equal, disregarding minor floating point imprecisions.
        /// </summary>
        /// <param name="a">The first number to check.</param>
        /// <param name="b">The second number to check.</param>
        /// <returns>Whether the input arguments are equal.</returns>
        public static bool ApproximatelyEqual(float a, float b)
        {
            bool result = ((a - b) * (a - b)) < 0.001f;
            return result;
        }

        public static IEnumerator StartAndEnterRound()
        {
            yield return ClickButton(ReadyButtonName);
            yield return StartRound();
        }

        public static IEnumerator ClickButton(string buttonName, float delay = 1f)
        {
            PressButton(buttonName);
            yield return new WaitForSeconds(delay);
        }

        /// <summary>
        /// This method simulates the user in the lobby switching to the "Server Settings" tab, and clicking "Start Round".
        /// </summary>
        /// <param name="delay">Time delay between actions.</param>
        /// <returns>IEnumerator for yielding in UnityTest.</returns>
        public static IEnumerator StartRound(float delay = 0.5f)
        {
            SetTabActive(ServerSettingsTabName); yield return new WaitForSeconds(delay);
            PressButton(StartRoundButtonName); yield return new WaitForSeconds(delay);
        }

        public static IEnumerator LateJoinRound()
        {
            // Fire up the game
            SetTabActive(ServerSettingsTabName); yield return new WaitForSeconds(1f);
            PressButton(StartRoundButtonName); yield return new WaitForSeconds(1f);

            // Give a few moments pause before we try and late join
            yield return new WaitForSeconds(5f);

            // Now we join.
            PressButton(EmbarkButtonName); yield return new WaitForSeconds(3f);

        }

        public static IEnumerator Embark()
        {
            PressButton(EmbarkButtonName); yield return new WaitForSeconds(3f);
        }

        public static IEnumerator FinishAndExitRound()
        {
            // Press and release the Escape key
            ScriptedInput input = UserInput.GetInputService() as ScriptedInput;
            input.HandleButton(CancelButton, true);
            yield return null;
            input.HandleButton(CancelButton, false);

            // Change to the server settings tab, and cancel the round
            SetTabActive(ServerSettingsTabName); yield return new WaitForSeconds(1f);
            PressButton(StartRoundButtonName); yield return new WaitForSeconds(1f);

            // Give a moment's pause before test formally concludes. (It takes a while for the round to reset).
            yield return new WaitForSeconds(4f);
        }

        public static void PressButton(string buttonName)
        {
            GetButton(buttonName).Press();
        }

        public static LabelButton GetButton(string buttonName)
        {
            LabelButton button = GameObject.Find(buttonName)?.GetComponent<LabelButton>();
            Assert.IsNotNull(button, $"Did not find a button in the scene called {buttonName}");
            return button;
        }

        public static void SetTabActive(string tabName)
        {
            GameObject.Find(tabName)?.GetComponent<Button>().onClick.Invoke();
        }
    }
}