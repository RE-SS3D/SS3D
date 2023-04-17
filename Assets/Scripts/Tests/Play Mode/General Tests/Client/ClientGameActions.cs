using System;
using System.Collections;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.IngameConsoleSystem.Commands;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Screens;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.TestTools;
using UnityEngine.Windows;
using SS3D.Systems.Inventory.Containers;
using System.Security.Cryptography;

namespace SS3D.Tests
{
    public class ClientGameActions : SpessClientPlayModeTest
    {
        public override IEnumerator UnitySetUp()
        {
            yield return base.UnitySetUp();
            yield return TestHelpers.StartAndEnterRound();
            yield return GetHumanoidController();
            yield return GetInteractionController();
        }

        public override void Setup()
        {
            base.Setup();

            leftMouseClick.performed += InteractionController.HandleRunPrimary;
            leftMouseClick.Enable();
        }

        public override IEnumerator UnityTearDown()
        {
            yield return TestHelpers.FinishAndExitRound();
            yield return base.UnityTearDown();
        }

        [UnityTest]
        public IEnumerator PlayerCanMoveInEachDirectionCorrectly_AsClientWithHost()
        {
            yield return PlayerCanMoveInEachDirectionCorrectly(HumanoidController);
        }

        /// <summary>
        /// Test which confirms that the player can correctly move in each of the eight directions.
        /// Note: this test is vulnerable to the player being blocked from movement by map features.
        /// </summary>
        /// <param name="controller">The player character.</param>
        public IEnumerator PlayerCanMoveInEachDirectionCorrectly(HumanoidController controller)
        {
            yield return MoveInDirection(controller, +1, 0);  // East
            yield return MoveInDirection(controller, -1, 0);  // West
            yield return MoveInDirection(controller, 0, +1);  // North
            yield return MoveInDirection(controller, 0, -1);  // South
            yield return MoveInDirection(controller, +1, +1); // Northeast
            yield return MoveInDirection(controller, -1, -1); // Southwest
            yield return MoveInDirection(controller, -1, +1); // Northwest
            yield return MoveInDirection(controller, +1, -1); // Southeast
        }

        private IEnumerator MoveInDirection(HumanoidController controller, float xInput = 0, float yInput = 0)
        {
            // Record the original position
            Vector3 originalPosition = controller.Position;

            yield return TestHelpers.MoveInDirection(this, xInput, yInput);
            // Record the final position
            Vector3 newPosition = controller.Position;

            // Ensure that position changes are what we expected based on applied input
            AssertCorrectPlayerPositionOnAxis(newPosition.x, originalPosition.x, xInput);    // x-axis (Maps to x input)
            AssertCorrectPlayerPositionOnAxis(newPosition.y, originalPosition.y, 0);         // y-axis (Vertical axis unaligned to input)
            AssertCorrectPlayerPositionOnAxis(newPosition.z, originalPosition.z, yInput);    // z-axis (Maps to y input)

            // Wait for a bit to separate the tests from each other
            yield return new WaitForSeconds(1f);
        }

        private void AssertCorrectPlayerPositionOnAxis(float newPointOnAxis, float originalPointOnAxis, float inputChangeOnAxis)
        {
            if (inputChangeOnAxis == 0)
            {
                // We did not add input to move on this particular axis, so the new and old positions on the axis should match.
                Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPointOnAxis, originalPointOnAxis),
                    $"Expected new position {newPointOnAxis} to equal old position {originalPointOnAxis}, but it did not.");
            }
            else if (inputChangeOnAxis > 0)
            {
                // We moved in a positive direction, so new position value should be greater.
                Assert.IsTrue(newPointOnAxis > originalPointOnAxis,
                    $"Expected new position {newPointOnAxis} to be greater than old position {originalPointOnAxis}, but it was not.");
            }
            else if (inputChangeOnAxis < 0)
            {
                // We moved in a negative direction, so new position value should be less.
                Assert.IsTrue(newPointOnAxis < originalPointOnAxis,
                    $"Expected new position {newPointOnAxis} to be less than old position {originalPointOnAxis}, but it was not.");
            }
        }

        /// <summary>
        /// Test that spawn an item and check if the player can drop it and pick it up with primary interaction.
        [UnityTest]
        public IEnumerator PlayerCanDropAndPickUpItem()
        {
            // Get local player position, interaction controller and put bikehorn in first hand available.
            var hand = TestHelpers.LocalPlayerSpawnItemInFirstHandAvailable(Data.Enums.ItemId.PDA);
            var playerPosition = TestHelpers.GetLocalPlayerPosition();

            yield return new WaitForSeconds(0.2f);

            // Drop item at a close position from local player
            var itemPosition = playerPosition;
            var camera = Subsystems.Get<CameraSystem>().PlayerCamera.GetComponent<Camera>();
            var target = camera.WorldToScreenPoint(itemPosition);
            
            var target2D = new Vector2(target.x, target.y) - new Vector2(-60, -60);
            Set(mouse.position, target2D);

            // Check that player can drop and pick up item again.
            Assert.That(!hand.Empty);
            yield return new WaitForSeconds(0.2f);
            Debug.Log("pressing left button " + target2D);
            PressAndRelease(mouse.leftButton);
            yield return new WaitForSeconds(0.2f);
            Assert.That(hand.Empty);
            yield return new WaitForSeconds(0.2f);
            PressAndRelease(mouse.leftButton);
            yield return new WaitForSeconds(0.1f);
            Assert.That(!hand.Empty);

            yield return new WaitForSeconds(1f);

        }
    }
}
