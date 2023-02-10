using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Core.Settings;
using SS3D.Systems.Rounds;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Coimbra;
using System.Diagnostics;
using FishNet.Managing;
using FishNet;
using SS3D.Systems.InputHandling;
using SS3D.Systems.Entities.Humanoid;
using System.Text;
using SS3D.Systems.Storage.UI;

namespace SS3D.Tests
{
    public class Issue0990_PropagatingPocketProblem_ClientPerspective : SpessClientPlayModeTest
    {
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

        }

        [UnitySetUp]
        public override IEnumerator UnitySetUp()
        {
            yield return base.UnitySetUp();
            yield return TestHelpers.StartAndEnterRound();
            yield return GetController();
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            yield return TestHelpers.FinishAndExitRound();
            yield return base.UnityTearDown();
        }

        [UnityTest]
        public IEnumerator PlayerHasTheSameNumberOfPocketsAfterEndingRoundAndStartingNewOne()
        {
            // Make sure the player has actually embarked
            yield return new WaitForSeconds(5f);

            // Count the number of container slots -> this will include pockets.
            //int initialNumberOfContainerSlots = GameObject.Find();
            
            // Exit the round, then restart shortly after.
            yield return TestHelpers.FinishAndExitRound();
            yield return new WaitForSeconds(5f);
            yield return TestHelpers.StartAndEnterRound();
            yield return new WaitForSeconds(5f);

            // Count the number of slots again -> it should be the same as the first time
            int subsequentNumberOfContainerSlots = GameObject.FindObjectsOfType<SingleItemContainerSlot>().Length;

            //Assert.IsTrue(initialNumberOfContainerSlots == subsequentNumberOfContainerSlots,
            //    $"Initially there were {initialNumberOfContainerSlots} slots, but now there are {subsequentNumberOfContainerSlots} slots");
        }

        [UnityTest]
        public IEnumerator y()
        {
            yield return null;
        }
    }
}
