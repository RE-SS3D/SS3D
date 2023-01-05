using NUnit.Framework;
using SS3D.Systems.Health;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EditorTests.Health
{
    public class HealthTests
    {
        #region Class variables
        /// <summary>
        /// Toggles whether any test-specific Debug.Log's are displayed in the console.
        /// </summary>
        private bool SHOW_DEBUG = false;
        #endregion

        #region Test set up
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }
        #endregion

        #region Tests

        /// <summary>
        /// Test to confirm that interactions can only be commenced when stamina is greater than zero, and will otherwise fail.
        /// </summary>
        [Test]
        [TestCase(9.99f, true)]
        [TestCase(10.01f, false)]
        public void CanCommenceInteractionOnlyWhenStaminaIsGreaterThatZero(float staminaToDeplete, bool expectedResult)
        {

            // ARRANGE
            IStamina sut = StaminaHelper.Create(StaminaType.Standard, 10f);

            // ACT
            sut.ConsumeStamina(staminaToDeplete);

            // ASSERT
            Assert.IsTrue(sut.CanCommenceInteraction == expectedResult);
        }

        /// <summary>
        /// Test to confirm that interactions can be continued when negative. However, they will not continue beyond 10% of the max
        /// stamina in deficit.
        /// </summary>
        [Test]
        [TestCase(10.99f, true)]
        [TestCase(11.01f, false)]
        public void CanContinueInteractionWithNegativeStaminaUntilNegativeTenPercent(float staminaToDeplete, bool expectedResult)
        {

            // ARRANGE
            IStamina sut = StaminaHelper.Create(StaminaType.Standard, 10f);

            // ACT
            sut.ConsumeStamina(staminaToDeplete);

            // ASSERT
            Assert.IsTrue(sut.CanContinueInteraction == expectedResult);
        }

        /// <summary>
        /// Test to confirm that reducing stamina will result in the correct value for Current property being returned.
        /// Note that Current should always be in the range of 0 to 1 (inclusive).
        /// </summary>
        [Test]
        [TestCase(0f, 1f)]
        [TestCase(7f, 0.3f)]
        [TestCase(100f, 0f)]
        public void ConsumeStaminaCorrectlyReducesTheStaminaValue(float staminaToDeplete, float expectedResult)
        {
            // ARRANGE
            IStamina sut = StaminaHelper.Create(StaminaType.Standard, 10f);

            // ACT
            sut.ConsumeStamina(staminaToDeplete);

            // ASSERT
            Assert.IsTrue(sut.Current == expectedResult);
        }

        /// <summary>
        /// Test to confirm that recharging stamina will result in the correct value for Current property being returned.
        /// Note that Current should always be in the range of 0 to 1 (inclusive).
        /// </summary>
        [Test]
        [TestCase(0f, 0f)]
        [TestCase(0.7f, 0.7f)]
        [TestCase(100f, 1f)]
        public void RechargingStaminaCorrectlyReducesTheStaminaValue(float secondsToRecharge, float expectedResult)
        {
            // ARRANGE
            IStamina sut = StaminaHelper.Create(StaminaType.Standard, 10f, 1f); // Set up stamina to fully recharge after 1 second.
            sut.ConsumeStamina(10f);                                            // Deplete all of the stamina

            // ACT
            sut.RechargeStamina(secondsToRecharge);

            // ASSERT
            Assert.IsTrue(sut.Current == expectedResult);
        }

        #endregion

        #region Helper functions

        #endregion
    }
}
