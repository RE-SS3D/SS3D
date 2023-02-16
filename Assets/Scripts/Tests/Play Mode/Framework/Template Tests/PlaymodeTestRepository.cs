using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.InputHandling;
using SS3D.Systems.Rounds;
using SS3D.UI.Buttons;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Tests
{
    /// <summary>
    /// This class is simply a container for methods representing UnityTests. These are not
    /// tests themselves but effectively hold all test logic. This allows for the tests to
    /// be easily run regardless of host or server/client configuration.
    /// </summary>
    public static class PlaymodeTestRepository
    {
        #region Constants
        private const string HorizontalAxis = "Horizontal";
        private const string VerticalAxis = "Vertical";
        private const string ReadyButtonName = "Ready";
        private const string ServerSettingsTabName = "Server Settings";
        private const string StartRoundButtonName = "Start Round";
        #endregion

        /// <summary>
        /// This test proxy simply ensures that the player is above the height of the station floor.
        /// It aims to detect the bug where the player spawns off the station.
        /// </summary>
        /// <param name="controller">The player character.</param>
        /// <param name="showDebug">Whether the position log (by frame) should be displayed in the console.</param>
        /// <returns>IEnumerator for use as a UnityTest.</returns>
        public static IEnumerator PlayerRemainsAboveStationLevelAfterSpawn(HumanoidController controller, bool showDebug = false)
        {
            // The height that we are checking
            const float StationHeight = 100f;

            string ckey = controller.GetComponent<Entity>().Ckey;

            
            // Log the fall profile for a couple of seconds.
            StringBuilder sb = new StringBuilder();
            float originalTime = Time.time;
            while (Time.time < originalTime + 2f)
            {
                sb.Append($"Time {Time.time - originalTime}: Player {ckey} position = {controller.Position}.\n");
                yield return null;
            }

            // Assert that the player is above the height of the station floor.
            if (showDebug) Debug.Log(sb.ToString());
            Assert.IsTrue(controller.Position.y > StationHeight, $"Player fell to {controller.Position.y} after spawn. Expected to remain above {StationHeight}.");
        }

        public static IEnumerator PlayersRemainAboveStationLevelAfterSpawn(GameObject[] players, bool showDebug = false)
        {
            // The height that we are checking
            const float StationHeight = 100f;

            // Give the players time to fall
            yield return new WaitForSeconds(10f);

            // Check how many players are above the height of the station floor.
            int numberOfPlayersAtCorrectHeight = 0;
            foreach (GameObject player in players)
            {
                if (player.transform.position.y > StationHeight)
                {
                    numberOfPlayersAtCorrectHeight++;
                }
            }

            Assert.IsTrue(numberOfPlayersAtCorrectHeight == players.Length, 
                $"Only {numberOfPlayersAtCorrectHeight} of {players.Length} players remained above station level of y = {StationHeight}.");
        }

        /// <summary>
        /// This test proxy simply tests the player moving in each of the eight cardinal directions,
        /// and that the player position is appropriately changed after each move. Please ensure that
        /// there are no objects blocking the player's path, or this test will likely fail.
        /// </summary>
        /// <param name="controller">The player character.</param>
        /// <returns>IEnumerator for use as a UnityTest.</returns>
        public static IEnumerator PlayerCanMoveInEachDirectionCorrectly(HumanoidController controller)
        {
            Vector3 originalPosition;
            Vector3 newPosition;

            // Move West.
            originalPosition = controller.Position;
            yield return TestHelpers.Move(HorizontalAxis, -1f);
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x < originalPosition.x);
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.z, originalPosition.z));

            // Move East.
            originalPosition = controller.Position;
            yield return TestHelpers.Move(HorizontalAxis, 1f);
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x > originalPosition.x);
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.z, originalPosition.z));

            // Move North.
            originalPosition = controller.Position;
            yield return TestHelpers.Move(VerticalAxis, -1f);
            newPosition = controller.Position;
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.x, originalPosition.x));
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(newPosition.z < originalPosition.z);

            // Move South.
            originalPosition = controller.Position;
            yield return TestHelpers.Move(VerticalAxis, 1f);
            newPosition = controller.Position;
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.x, originalPosition.x));
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(newPosition.z > originalPosition.z);

            // Move Northeast.
            originalPosition = controller.Position;
            yield return TestHelpers.Move(new[] { VerticalAxis, HorizontalAxis }, new[] { -1f, -1f });
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x < originalPosition.x);
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(newPosition.z < originalPosition.z);

            // Move Southwest.
            originalPosition = controller.Position;
            yield return TestHelpers.Move(new[] { VerticalAxis, HorizontalAxis }, new[] { 1f, 1f });
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x > originalPosition.x);
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(newPosition.z > originalPosition.z);

            // Move Northwest.
            originalPosition = controller.Position;
            yield return TestHelpers.Move(new[] { VerticalAxis, HorizontalAxis }, new[] { 1f, -1f });
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x < originalPosition.x);
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(newPosition.z > originalPosition.z);

            // Move Southeast.
            originalPosition = controller.Position;
            yield return TestHelpers.Move(new[] { VerticalAxis, HorizontalAxis }, new[] { -1f, 1f });
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x > originalPosition.x);
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(newPosition.z < originalPosition.z);

            yield break;
        }

        public static IEnumerator ReadyToggleButtonCorrectlyFunctionsWhenClicked()
        {
            // ARRANGE
            // Check the number of players currently ready in the game
            ReadyPlayersSystem readyPlayersSystem = SystemLocator.Get<ReadyPlayersSystem>();
            int originalReadyPlayers = readyPlayersSystem.Count;

            // Check the colour of the ready button
            LabelButton button = TestHelpers.GetButton(ReadyButtonName);
            Color originalButtonColor = button.GetComponent<Image>().color;

            // ACT #1 - Press the button to set us to Ready, then wait for a moment
            button.Press();
            yield return new WaitForSeconds(2f);

            // ASSERT #1: Player should now be Ready.
            // Check: The ready player count should have incremented.
            Assert.IsTrue(readyPlayersSystem.Count == originalReadyPlayers + 1, $"Number of ready players was not incremented when this player became ready.");
            // Check: The ready button color should have changed.
            Assert.IsTrue(originalButtonColor != button.GetComponent<Image>().color, $"The button colour did not change when the button was clicked");


            // ACT #2 - Press the button to set us to Not Ready, then wait for a moment
            button.Press();
            yield return new WaitForSeconds(2f);

            // ASSERT #2: Player should now be Not Ready
            // Check: The ready player count should be back to having the original number of ready players.
            Assert.IsTrue(readyPlayersSystem.Count == originalReadyPlayers, $"Number of ready players was not decremented when this player became not ready.");
            // Check: The ready button color should have changed back to the original.
            Assert.IsTrue(originalButtonColor == button.GetComponent<Image>().color, $"The button colour did not change when the button was clicked");
        }
    }
}