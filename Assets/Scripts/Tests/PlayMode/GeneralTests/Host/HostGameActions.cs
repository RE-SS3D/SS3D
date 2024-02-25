using System.Collections;
using System.Threading;
using NUnit.Framework;
using SS3D.Core.Settings;
using SS3D.Networking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace SS3D.Tests
{

    public class HostGameActions : PlayModeTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            if (!setUpOnce)
            {
                yield return LoadAndSetInLobby(NetworkType.Host);
                setUpOnce = true;
            }
            yield return SetInGame();
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return TestHelpers.FinishAndExitRound();
        }

        [UnityTest]
        public IEnumerator PlayerRemainsAboveStationLevelAfterSpawn([ValueSource("Iterations")] int iteration)
        {
            LogAssert.ignoreFailingMessages = true;
            yield return PlaymodeTestRepository.PlayerRemainsAboveStationLevelAfterSpawn(HumanoidController);
        }

        [UnityTest]
        public IEnumerator PlayerCanMoveInEachDirectionCorrectly()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return PlaymodeTestRepository.PlayerCanMoveInEachDirectionCorrectly(this, HumanoidController);
        }

        protected override bool UseMockUpInputs()
        {
            return true;
        }
    }
}
