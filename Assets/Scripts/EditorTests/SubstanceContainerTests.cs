using NUnit.Framework;
using SS3D.Substances;
using SS3D.Systems.Health;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubstanceContainerTests
{

    private SubstanceContainerActor.SubstanceContainer _container;
    private Substance _beer; 
    
    /// <summary>
    /// Test to confirm that interactions can only be commenced when stamina is greater than zero, and will otherwise fail.
    /// </summary>
    /// 
    [SetUp]
    public void SetUp()
    {
        _container = new SubstanceContainerActor.SubstanceContainer(200f);
        _beer = ScriptableObject.CreateInstance<Substance>();
        _beer.Color = Color.red;
        _beer.MillilitersPerMole = 50;
        _beer.Type = SubstanceType.Beer;
    }

    [TearDown]
    public void TearDown()
    {

    }

    [Test]
    public void CantAddMoreWhenSubstanceContainerIsFull()
    {
        _container.AddSubstance(_beer, int.MaxValue );
        float currentVolume = _container.CurrentVolume;
        Assert.AreEqual(currentVolume, _container.Volume );
    }

    [Test]
    public void CantRemoveMoreWhenSubstanceContainerIsEmpty()
    {
        _container.RemoveSubstance(_beer, int.MaxValue );
        float currentVolume = _container.CurrentVolume;
        Assert.AreEqual(currentVolume, 0);
    }

    [Test]
    public void CantAddSubstanceWhenLocked()
    {
        var containerLocked = new SubstanceContainerActor.SubstanceContainer(200f, true);
        _container.AddSubstance(_beer, int.MaxValue);
        float currentVolume = _container.CurrentVolume;
        Assert.AreEqual(currentVolume, 0);
    }




}
