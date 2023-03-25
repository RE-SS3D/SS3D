using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace SS3D.Tests
{

    public class HostGameActions : SpessHostPlayModeTest
    {
        private const string HorizontalAxis = "Horizontal";
        private const string VerticalAxis = "Vertical";

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

        //[UnityTest]
        public IEnumerator PlayerRemainsAboveStationLevelAfterSpawn([ValueSource("Iterations")] int iteration)
        {
            yield return PlaymodeTestRepository.PlayerRemainsAboveStationLevelAfterSpawn(controller);
        }

        //[UnityTest]
        public IEnumerator PlayerCanMoveInEachDirectionCorrectly()
        {
            yield return null;
        }
    }
}
