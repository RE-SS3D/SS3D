using NUnit.Framework;
using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Linq;
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
        /// This test proxy checks whether there are the same number of pocket AttachedContainers
        /// after the round restarts. Aims to validate Issue #990: Propagating Pocket Problem.
        /// Originally counted the (now condemned/deleted) SingleItemContainerSlot UI elements;
        /// counts the underlying AttachedContainer data instead since the storage redesign
        /// (Documents/architecture/2026-07_inventory-storage-redesign.md).
        /// </summary>
        /// <returns>IEnumerator for use as a UnityTest.</returns>
        public static IEnumerator Issue0990_PlayerHasTheSameNumberOfPocketsAfterEndingRoundAndStartingNewOne(bool useProgrammaticRoundControl = false)
        {
            yield return new WaitForSeconds(5f);

            int initialNumberOfPockets = CountPocketContainers();

            if (useProgrammaticRoundControl)
            {
                ServerHelpers.ChangeRoundState(false);
                yield return new WaitForSeconds(8f);

                ServerHelpers.SetPlayerReadiness("john", true);
                ServerHelpers.ChangeRoundState(true);
                yield return new WaitForSeconds(8f);
                yield return TestHelpers.Embark();
            }
            else
            {
                yield return TestHelpers.FinishAndExitRound();
                yield return new WaitForSeconds(5f);
                yield return TestHelpers.StartAndEnterRound();
            }

            yield return new WaitForSeconds(5f);

            int subsequentNumberOfPockets = CountPocketContainers();

            Assert.AreEqual(
                initialNumberOfPockets,
                subsequentNumberOfPockets,
                $"Initially there were {initialNumberOfPockets} pockets, but now there are {subsequentNumberOfPockets} pockets");
        }

        private static int CountPocketContainers()
        {
            return Object.FindObjectsByType<AttachedContainer>(FindObjectsSortMode.None)
                .Count(container => container.Type == ContainerType.Pocket);
        }
    }
}