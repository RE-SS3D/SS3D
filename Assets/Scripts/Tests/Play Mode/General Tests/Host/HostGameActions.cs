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

        [UnityTest]
        public IEnumerator PlayerRemainsAboveStationLevelAfterSpawn([ValueSource("Iterations")] int iteration)
        {
            yield return PlaymodeTestRepository.PlayerRemainsAboveStationLevelAfterSpawn(controller);
        }

        [UnityTest]
        public IEnumerator PlayerCanMoveInEachDirectionCorrectly()
        {
            //yield return PlaymodeTestRepository.PlayerCanMoveInEachDirectionCorrectly(controller);

            Vector3 originalPosition;
            Vector3 newPosition;

            InputAction action = new InputAction("Movement", binding: "<Keyboard>/aKey", interactions: "Left");

            action.Enable();

            // Move West.
            originalPosition = controller.Position;
            Press(keyboard.aKey);
            yield return new WaitForSeconds(1f);
            Release(keyboard.aKey);
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x < originalPosition.x);
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.z, originalPosition.z));
            yield break;
        }
    }
}
