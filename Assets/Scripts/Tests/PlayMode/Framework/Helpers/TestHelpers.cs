using FishNet;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Systems.Entities;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.UI.Buttons;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
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

        public static IEnumerator StartAndEnterRound(float delay = 0f)
        {
            yield return StartRound();
            yield return new WaitForSeconds(delay);
            yield return PressButtonWhenAvailable(EmbarkButtonName);
        }

        /// <summary>
        /// This method simulates the user in the lobby switching to the "Server Settings" tab, and clicking "Start Round".
        /// </summary>
        /// <param name="delay">Time delay between actions.</param>
        /// <returns>IEnumerator for yielding in UnityTest.</returns>
        public static IEnumerator StartRound(float delay = 0.5f)
        {
            SetTabActive(ServerSettingsTabName);

            yield return new WaitForSeconds(delay);
            PressButton(StartRoundButtonName);

            yield return new WaitForSeconds(delay);
        }

        public static IEnumerator LateJoinRound()
        {
            // Fire up the game
            SetTabActive(ServerSettingsTabName);

            yield return new WaitForSeconds(1f);
            PressButton(StartRoundButtonName);

            yield return new WaitForSeconds(1f);

            // Give a few moments pause before we try and late join
            yield return new WaitForSeconds(5f);

            // Now we join.
            PressButton(EmbarkButtonName);

            yield return new WaitForSeconds(3f);

        }

        public static IEnumerator Embark()
        {
            PressButton(EmbarkButtonName);

            yield return new WaitForSeconds(3f);
        }

        public static IEnumerator ContinueFreePlayUntilControlAltBackspacePressed()
        {
            bool controlPressed;
            bool altPressed;
            bool backspacePressed;
            bool endTestKeyCombo = false;

            Debug.Log("Entering Free Play. Press Control + Alt + Backspace while focused on the game window in Unity to finish Free Play.");

            while (!endTestKeyCombo)
            {
                yield return null;
                controlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                backspacePressed = Input.GetKey(KeyCode.Backspace);
                endTestKeyCombo = controlPressed && altPressed && backspacePressed;
            }

            Debug.Log("Ending Free Play.");
        }

        public static IEnumerator FinishAndExitRound()
        {
            // Press and release the Escape key
            //TODO: ScriptedInput input = UserInput.GetInputService() as ScriptedInput;
            //input.HandleButton(CancelButton, true);
            yield return null;

            //input.HandleButton(CancelButton, false);

            // Change to the server settings tab, and cancel the round
            SetTabActive(ServerSettingsTabName);

            yield return new WaitForSeconds(1f);
            PressButton(StartRoundButtonName);

            yield return new WaitForSeconds(1f);

            // Give a moment's pause before test formally concludes. (It takes a while for the round to reset).
            yield return new WaitForSeconds(4f);
        }

        public static void PressButton(string buttonName)
        {
            GetButton(buttonName).Press();
        }

        public static IEnumerator PressButtonWhenAvailable(string buttonName, float timeout = 15f)
        {
            LabelButton button = GetButton(buttonName);
            float startTime = Time.time;

            while (button == null)
            {
                Assert.IsTrue(Time.time < startTime + timeout, $"Timeout of {timeout} reached when trying to press {buttonName} button");

                yield return new WaitForSeconds(1f);
                button = GetButton(buttonName);
            }

            button.Press();
        }

        public static LabelButton GetButton(string buttonName)
        {
            LabelButton button = GameObject.Find(buttonName)?.GetComponent<LabelButton>();

            return button;
        }

        public static void SetTabActive(string tabName)
        {
            GameObject.Find(tabName)?.GetComponent<Button>().onClick.Invoke();
        }

        public static IEnumerator MoveInDirection(PlayModeTest fixture, float xInput = 0, float yInput = 0, float time = 1f)
        {
            // Apply the movement input
            fixture.Set((AxisControl)fixture.InputDevice["Movement/x"], xInput);
            fixture.Set((AxisControl)fixture.InputDevice["Movement/y"], yInput);

            // Wait for a bit to give player time to move
            yield return new WaitForSeconds(time);

            // Remove the applied input
            fixture.Set((AxisControl)fixture.InputDevice["Movement/x"], 0);
            fixture.Set((AxisControl)fixture.InputDevice["Movement/y"], 0);
        }

        public static AttachedContainer LocalPlayerSpawnItemInFirstHandAvailable(string item)
        {
            ItemSubSystem itemSystem = SubSystems.Get<ItemSubSystem>();
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            entitySystem.TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity);
            HumanInventory inventory = entity.gameObject.GetComponent<HumanInventory>();

            foreach (Hand hand in inventory.Hands.PlayerHands.Where(hand => hand.Container.Empty))
            {
                Item itemToSpawn = Assets.Get<GameObject>(AssetDatabases.Items, item).GetComponent<Item>();

                itemSystem.CmdSpawnItemInContainer(itemToSpawn, hand.Container);

                return hand.Container;
            }

            Debug.Log("No free hands.");

            return null;
        }

        public static InteractionController GetLocalInteractionController()
        {
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            entitySystem.TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity);

            return entity.gameObject.GetComponent<InteractionController>();
        }

        public static Vector3 GetLocalPlayerPosition()
        {
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            entitySystem.TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity);

            return entity.gameObject.transform.position;
        }
    }
}