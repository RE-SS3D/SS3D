using NUnit.Framework;
using SS3D.Systems.Stamina;

namespace EditorTests
{
    public class StaminaTests
    {
        [Test]
        [TestCase(9.99f, true)]
        [TestCase(10.01f, false)]
        public void CanCommenceInteractionOnlyWhenStaminaIsGreaterThatZero(float staminaToDeplete, bool expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f);
            sut.ConsumeStamina(staminaToDeplete);
            Assert.AreEqual(expectedResult, sut.CanCommenceInteraction);
        }

        [Test]
        [TestCase(10.99f, true)]
        [TestCase(11.01f, false)]
        public void CanContinueInteractionWithNegativeStaminaUntilNegativeTenPercent(float staminaToDeplete, bool expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f);
            sut.ConsumeStamina(staminaToDeplete);
            Assert.AreEqual(expectedResult, sut.CanContinueInteraction);
        }

        [Test]
        [TestCase(0f, 1f)]
        [TestCase(7f, 0.3f)]
        [TestCase(100f, 0f)]
        public void ConsumeStaminaCorrectlyReducesTheStaminaValue(float staminaToDeplete, float expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f);
            sut.ConsumeStamina(staminaToDeplete);
            Assert.AreEqual(expectedResult, sut.Current);
        }

        [Test]
        [TestCase(0f, 0f)]
        [TestCase(0.7f, 0.7f)]
        [TestCase(100f, 1f)]
        public void RechargingStaminaCorrectlyReducesTheStaminaValue(float secondsToRecharge, float expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f, 1f);
            sut.ConsumeStamina(10f);
            sut.RechargeStamina(secondsToRecharge);
            Assert.AreEqual(expectedResult, sut.Current);
        }
    }
}
