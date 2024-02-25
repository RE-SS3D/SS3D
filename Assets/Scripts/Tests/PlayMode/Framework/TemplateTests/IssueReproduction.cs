using NUnit.Framework;
using SS3D.Systems.Inventory.UI;
using System.Collections;
using UnityEngine;

namespace SS3D.Tests
{
    /// <summary>
    /// This class is simply a container for methods representing UnityTests. These represent
    /// existing or former issues as recorded on GitHub. Successful closure of existing issues
    /// should include review of the corresponding test cases. Please note that these are not
    /// tests themselves but effectively hold all test logic. This allows for the tests to
    /// be easily run regardless of host or server/client configuration.
    /// </summary>
    public static class IssueReproduction
    {
        #region Constants
        private const string HorizontalAxis = "Horizontal";
        private const string VerticalAxis = "Vertical";
        private const string ReadyButtonName = "Ready";
        private const string ServerSettingsTabName = "Server Settings";
        private const string StartRoundButtonName = "Start Round";
        #endregion

        /// <summary>
        /// This test proxy checks whether there are the same number of pockets (well, SingleItemContainerSlots)
        /// after the round restarts. Aims to validate Issue #990: Propagating Pocket Problem.
        /// </summary>
        /// <returns>IEnumerator for use as a UnityTest.</returns>
        public static IEnumerator Issue0990_PlayerHasTheSameNumberOfPocketsAfterEndingRoundAndStartingNewOne()
        {
            // Make sure the player has actually embarked
            yield return new WaitForSeconds(5f);

            // Count the number of container slots -> this will include pockets.
            int initialNumberOfContainerSlots = Object.FindObjectsOfType(typeof(SingleItemContainerSlot)).Length;

            // Exit the round, then restart shortly after.
            yield return TestHelpers.FinishAndExitRound();
            yield return new WaitForSeconds(5f);
            yield return TestHelpers.StartAndEnterRound();
            yield return new WaitForSeconds(5f);

            // Count the number of slots again -> it should be the same as the first time
            int subsequentNumberOfContainerSlots = Object.FindObjectsOfType(typeof(SingleItemContainerSlot)).Length;

            Assert.IsTrue(initialNumberOfContainerSlots == subsequentNumberOfContainerSlots,
                $"Initially there were {initialNumberOfContainerSlots} slots, but now there are {subsequentNumberOfContainerSlots} slots");
        }
    }
}