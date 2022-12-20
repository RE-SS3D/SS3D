using NUnit.Framework;
using SS3D.Systems.Gamemodes;
using UnityEngine;

namespace EditorTests
{
    public class GamemodeTests
    {
        #region Class variables

        #endregion

        #region Test set up
        [OneTimeSetUp]
        public void SetUp()
        {

        }
        #endregion

        #region Tests

        /// <summary>
        /// Test to confirm that the GamemodeObjective.Succeed() method correctly sets the state.
        /// </summary>
        [Test]
        public void SucceedingAnObjectiveShouldSetStatusToSuccess()
        {

            // ARRANGE
            GamemodeObjective sut = ScriptableObject.CreateInstance<GamemodeObjective>();
            Assert.IsFalse(sut.Succeeded);

            // ACT
            sut.Succeed();

            // ASSERT
            Assert.IsTrue(sut.Succeeded);
        }

        /// <summary>
        /// Test to confirm that the GamemodeObjective.Fail() method correctly sets the state.
        /// </summary>
        [Test]
        public void FailingAnObjectiveShouldSetStatusToFailed()
        {

            // ARRANGE
            GamemodeObjective sut = ScriptableObject.CreateInstance<GamemodeObjective>();
            Assert.IsFalse(sut.Failed);

            // ACT
            sut.Fail();

            // ASSERT
            Assert.IsTrue(sut.Failed);
        }

        /// <summary>
        /// Test to confirm that the GamemodeObjective.Succeed() method does not change the
        /// status if the current status is not InProgress.
        /// </summary>
        [Test]
        [TestCase(ObjectiveStatus.Cancelled)]
        [TestCase(ObjectiveStatus.Failed)]

        public void ObjectiveDoesNotSucceedUnlessItIsFirstInProgress(ObjectiveStatus startingStatus)
        {

            // ARRANGE
            if (startingStatus == ObjectiveStatus.InProgress || startingStatus == ObjectiveStatus.Success) return;
            GamemodeObjective sut = ScriptableObject.CreateInstance<GamemodeObjective>();
            sut.SetStatus(startingStatus);

            // ACT
            sut.Succeed();

            // ASSERT
            Assert.IsTrue(sut.Status == startingStatus);
        }

        /// <summary>
        /// Test to confirm that the GamemodeObjective.Fail() method does not change the
        /// status if the current status is not InProgress.
        /// </summary>
        [Test]
        [TestCase(ObjectiveStatus.Cancelled)]
        [TestCase(ObjectiveStatus.Success)]
        public void ObjectiveDoesNotFailUnlessItIsFirstInProgress(ObjectiveStatus startingStatus)
        {

            // ARRANGE
            if (startingStatus == ObjectiveStatus.InProgress || startingStatus == ObjectiveStatus.Failed) return;
            GamemodeObjective sut = ScriptableObject.CreateInstance<GamemodeObjective>();
            sut.SetStatus(startingStatus);

            // ACT
            sut.Fail();

            // ASSERT
            Assert.IsTrue(sut.Status == startingStatus);
        }

        /// <summary>
        /// Test to confirm that the GamemodeObjective.InitializeObjective() method changes the current status
        /// to InProgress.
        /// </summary>
        [Test]
        [TestCase(ObjectiveStatus.Failed)]
        [TestCase(ObjectiveStatus.Cancelled)]
        [TestCase(ObjectiveStatus.Success)]
        public void InitializeObjectiveSetsStatusToInProgress(ObjectiveStatus startingStatus)
        {

            // ARRANGE
            if (startingStatus == ObjectiveStatus.InProgress) return;
            GamemodeObjective sut = ScriptableObject.CreateInstance<GamemodeObjective>();
            sut.SetStatus(startingStatus);

            // ACT
            sut.InitializeObjective();

            // ASSERT
            Assert.IsTrue(sut.InProgress);
        }
        #endregion

        #region Helper functions

        #endregion
    }
}
