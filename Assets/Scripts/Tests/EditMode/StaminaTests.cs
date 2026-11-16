using NUnit.Framework;
using SS3D.Systems.Stamina;
using UnityEngine;

namespace EditorTests
{
    public class StaminaTests
    {
        [Test]
        public void CanCommenceInteraction_AlwaysTrue_EvenWhenEmpty()
        {
            IStamina sut = StaminaFactory.Create(10f);
            sut.ConsumeStamina(10f);
            Assert.IsTrue(sut.CanCommenceInteraction);
            Assert.IsTrue(sut.CanContinueInteraction);
        }

        [Test]
        public void ConsumeStamina_TracksOverdrawPastEmpty()
        {
            IStamina sut = StaminaFactory.Create(10f);
            sut.ConsumeStamina(12f);
            Assert.AreEqual(0f, sut.CurrentAbsolute, 0.001f);
            Assert.AreEqual(2f, sut.LastOverdraw, 0.001f);
        }

        [Test]
        [TestCase(0f, 1f)]
        [TestCase(7f, 0.3f)]
        [TestCase(100f, 0f)]
        public void ConsumeStaminaCorrectlyReducesTheStaminaValue(float staminaToDeplete, float expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f);
            sut.ConsumeStamina(staminaToDeplete);
            Assert.AreEqual(expectedResult, sut.Current, 0.001f);
        }

        [Test]
        [TestCase(0f, 0f)]
        [TestCase(0.7f, 0.7f)]
        [TestCase(100f, 1f)]
        public void RechargingStaminaCorrectlyRestoresTheStaminaValue(float secondsToRecharge, float expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f, 1f);
            sut.ConsumeStamina(10f);
            sut.RechargeStamina(secondsToRecharge);
            Assert.AreEqual(expectedResult, sut.Current, 0.001f);
        }

        [Test]
        public void ApplyModifiers_ReducesMaxFromEncumbrance()
        {
            IStamina sut = StaminaFactory.Create(10f, 0.08f);
            sut.ApplyModifiers(maxScale: 0.5f, regenScale: 1f);
            Assert.AreEqual(5f, sut.Max, 0.001f);
        }

        [Test]
        public void ApplyModifiers_PreservesAbsoluteCurrentAcrossRefresh()
        {
            IStamina sut = StaminaFactory.Create(10f, 0.08f);
            sut.ConsumeStamina(4f);
            float before = sut.CurrentAbsolute;
            sut.ApplyModifiers(maxScale: 0.5f, regenScale: 1f);
            sut.ApplyModifiers(maxScale: 0.5f, regenScale: 1f);
            Assert.AreEqual(Mathf.Min(before, sut.Max), sut.CurrentAbsolute, 0.001f);
        }

        [Test]
        public void ExertionPenalty_IsOneWhenEmpty()
        {
            IStamina sut = StaminaFactory.Create(10f);
            sut.ConsumeStamina(10f);
            Assert.AreEqual(1f, sut.ExertionPenalty, 0.001f);
        }
    }
}
