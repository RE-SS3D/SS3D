using FishNet;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Systems.Entities;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Rounds;
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

        // Round flow goes through the server (ServerHelpers), not lobby buttons: these tests cover
        // gameplay, and the lobby UI has its own pilots (Testing/ui).

        /// <summary>Starts a round, waits until it is Ongoing, then embarks the local player after <paramref name="delay"/>.</summary>
        public static IEnumerator StartAndEnterRound(float delay = 0f)
        {
            yield return StartRound();
            yield return ServerHelpers.WaitForRoundState(RoundState.Ongoing, 60f);
            yield return new WaitForSeconds(delay);
            yield return Embark();
        }

        /// <summary>Requests a round start, as an administrator's Start Round would.</summary>
        public static IEnumerator StartRound(float delay = 0.5f)
        {
            ServerHelpers.ChangeRoundState(true);
            yield return new WaitForSeconds(delay);
        }

        /// <summary>Starts a round without readying up, then joins it late.</summary>
        public static IEnumerator LateJoinRound()
        {
            ServerHelpers.SetPlayerReadiness(ServerHelpers.LocalCkey, false);
            yield return StartRound();
            yield return ServerHelpers.WaitForRoundState(RoundState.Ongoing, 60f);
            yield return new WaitForSeconds(5f);
            yield return Embark();
        }

        /// <summary>Joins the Ongoing round as the local player (no-op if already spawned), then lets the body settle.</summary>
        public static IEnumerator Embark()
        {
            yield return ServerHelpers.EmbarkPlayer(ServerHelpers.LocalCkey);
            // A fresh body drops onto the floor after spawning; movement tests measure positions.
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
            yield return TryFinishAndExitRound();
        }

        /// <summary>
        /// Stops an active round and waits until it is Stopped. Safe to call from TearDown after a failed SetUp.
        /// </summary>
        public static IEnumerator TryFinishAndExitRound()
        {
            yield return ServerHelpers.StopRound();
        }

        public static void PressButton(string buttonName)
        {
            LabelButton button = GetButton(buttonName);
            if (button == null)
            {
                return;
            }

            button.Press();
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
                Item itemToSpawn = Assets.Get<GameObject>(AssetDatabases.Items, item)?.GetComponent<Item>();

                if (InstanceFinder.IsServer)
                {
                    itemSystem.SpawnItemInContainer(itemToSpawn.Name, hand.Container);
                }
                else
                {
                    itemSystem.CmdSpawnItemInContainer(itemToSpawn, hand.Container);
                }

                return hand.Container;
            }

            Debug.Log("No free hands.");

            return null;
        }

        /// <summary>The local player's InteractionController, or null until the owned entity is registered.</summary>
        public static InteractionController GetLocalInteractionController()
        {
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            if (!entitySystem.TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity))
            {
                return null;
            }

            return entity.GetComponent<InteractionController>();
        }

        public static Vector3 GetLocalPlayerPosition()
        {
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            entitySystem.TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity);

            return entity.gameObject.transform.position;
        }
    }
}