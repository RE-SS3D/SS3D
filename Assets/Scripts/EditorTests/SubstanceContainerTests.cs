using NUnit.Framework;
using SS3D.Substances;
using SS3D.Systems.Health;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubstanceContainerTests
{

    private SubstanceContainerActor.SubstanceContainer _container;
    /// <summary>
    /// Test to confirm that interactions can only be commenced when stamina is greater than zero, and will otherwise fail.
    /// </summary>
    /// 
    [SetUp]
    public void SetUp()
    {
        _container = new SubstanceContainerActor.SubstanceContainer(200f);
    }

    [TearDown]
    public void TearDown()
    {

    }

    [Test]
    public void CantAddMoreWhenSubstanceContainerIsFull()
    {
        _container.AddSubstance(new Substance(), );
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
        IStamina sut = StaminaFactory.Create(10f);

        sut.ConsumeStamina(staminaToDeplete);

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
        IStamina sut = StaminaFactory.Create(10f);

        sut.ConsumeStamina(staminaToDeplete);

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
        IStamina sut = StaminaFactory.Create(10f, 1f);   // Set up stamina to fully recharge after 1 second.
        sut.ConsumeStamina(10f);                        // Deplete all of the stamina

        sut.RechargeStamina(secondsToRecharge);

        Assert.IsTrue(sut.Current == expectedResult);
    }
}
