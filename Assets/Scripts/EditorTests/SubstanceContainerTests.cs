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
    private Substance _water;

    /// <summary>
    /// Test to confirm that interactions can only be commenced when stamina is greater than zero, and will otherwise fail.
    /// </summary>
    /// 
    [SetUp]
    public void SetUp()
    {
        _container = new SubstanceContainerActor.SubstanceContainer(10000f);
        _beer = ScriptableObject.CreateInstance<Substance>();
        _beer.Color = Color.red;
        _beer.MillilitersPerMole = 50;
        _beer.Type = SubstanceType.Beer;

        _water = ScriptableObject.CreateInstance<Substance>();
        _water.Color = Color.yellow;
        _water.MillilitersPerMole = 35;
        _water.Type = SubstanceType.Water;
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
        containerLocked.AddSubstance(_beer, int.MaxValue);
        float currentVolume = containerLocked.CurrentVolume;
        Assert.AreEqual(currentVolume, 0);
    }

    [Test]
    public void RemoveCorrectAmountOfEachSubstanceWhenRemovingMoles()
    {
        _container.AddSubstance(_beer, 10);
        _container.AddSubstance(_water, 5);
        _container.RemoveMoles(1.5f);
        float waterMole = _container.Substances.Find(x => x.Substance.Type == _water.Type).Moles;
        float beerMole = _container.Substances.Find(x => x.Substance.Type == _beer.Type).Moles;
        Assert.AreEqual(9f, beerMole);
        Assert.AreEqual(4.5f, waterMole);
    }




}
