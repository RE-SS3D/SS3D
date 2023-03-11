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
            input.Press(keyboard.aKey);
            yield return new WaitForSeconds(1f);
            input.Release(keyboard.aKey);
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
    }
}
