using System;
using System.Collections;
using NUnit.Framework;
using SS3D.Systems.Entities.Humanoid;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    public class ClientGameActions : SpessClientPlayModeTest
    {
        public override IEnumerator UnitySetUp()
        {
            yield return base.UnitySetUp();
            yield return TestHelpers.StartAndEnterRound();
            yield return GetController();
        }

        public override IEnumerator UnityTearDown()
        {
            yield return TestHelpers.FinishAndExitRound();
            yield return base.UnityTearDown();
        }

        /*
        [UnityTest]
        public IEnumerator PlayerRemainsAboveStationLevelAfterSpawn([ValueSource("Iterations")] int iteration)
        {
            yield return PlaymodeTestRepository.PlayerRemainsAboveStationLevelAfterSpawn(controller);
        }
        */

        [UnityTest]
        public IEnumerator PlayerCanMoveInEachDirectionCorrectly_AsClientWithHost()
        {
            yield return PlayerCanMoveInEachDirectionCorrectly(controller);
        }

        public IEnumerator PlayerCanMoveInEachDirectionCorrectly(HumanoidController controller)
        {
            Vector3 originalPosition;
            Vector3 newPosition;

            // Move West.
            originalPosition = controller.Position;
            //Debug.Log(inputDevice.allControls);
            //foreach (var control in inputDevice.allControls)
            //{
            //    Debug.Log($"Name = {control.name}, ValueType = {control.valueType}, Variants = {control.variants}, Layout = {control.layout}, Path = {control.path}");
                
            //}

            //Set(inputDevice, )
            //Press((ButtonControl)inputDevice["Movement/up"]);
            //Set((AxisControl)inputDevice["Movement"], 1);
            //Set((AxisControl)inputDevice["gobbledygook"], 1);


            //Set((AxisControl)inputDevice, "/Controls/Movement", 1f);
            //Press(keyboard.aKey);
            //yield return new WaitForSeconds(1f);
            //Release(keyboard.aKey);

            //Press(keyboard.wKey);
            yield return new WaitForSeconds(1f);
            //Release(keyboard.wKey);

            //Press(keyboard.sKey);
            yield return new WaitForSeconds(1f);
            //Release(keyboard.sKey);

            //Press(keyboard.dKey);
            yield return new WaitForSeconds(1f);
            //Release(keyboard.dKey);

            //Release((ButtonControl)inputDevice["up"]);
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x < originalPosition.x);
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(TestHelpers.ApproximatelyEqual(newPosition.z, originalPosition.z));

            /*
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
            */
        }
    }
}
