using System.Collections;
using UnityEngine.TestTools;
using SS3D.Networking;

namespace SS3D.Tests
{
    public class ClientGameActions : PlayModeTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LoadAndSetInGame(NetworkType.Client);
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return TestHelpers.FinishAndExitRound();
            KillAllBuiltExecutables();
        }

        /// <summary>
        /// Test which confirms that the player can correctly move in each of the eight directions.
        /// Note: this test is vulnerable to the player being blocked from movement by map features.
        /// </summary>
        /// <param name="controller">The player character.</param>
        [UnityTest]
        public IEnumerator PlayerCanMoveInEachDirectionCorrectly()
        {
            yield return PlaymodeTestRepository.PlayerCanMoveInEachDirectionCorrectly(this, HumanoidController);
        }

        /// <summary>
        /// Test that spawn an item and check if the player can drop it and pick it up with primary interaction.
        [UnityTest]
        public IEnumerator PlayerCanDropAndPickUpItem()
        {
            yield return PlaymodeTestRepository.PlayerCanDropAndPickUpItem(this);
        }

        protected override bool UseMockUpInputs()
        {
            return true;
        }
    }
}
