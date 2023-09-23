using NUnit.Framework;
using SS3D.Substances;
using SS3D.Systems.Substances;
using System.Linq;
using UnityEngine;

namespace Tests.Edit_Mode
{
    /// <summary>
    /// Set of tests for the substanceContainer class
    /// </summary>
    public class SubstanceContainerTests
    {
        private SubstanceContainer _container;
        private SubstanceContainer _containerLocked;
        private Substance _beer;
        private Substance _water;

        [SetUp]
        public void SetUp()
        {
            GameObject go = new GameObject();
            _container = go.AddComponent<SubstanceContainer>();
            _container.Init(10000f, false);

            GameObject go2 = new GameObject();
            _containerLocked = go2.AddComponent<SubstanceContainer>();
            _containerLocked.Init(10000f, true);


            _beer = ScriptableObject.CreateInstance<Substance>();
            _beer.Color = Color.red;
            _beer.MillilitersPerMole = 50;
            _beer.Type = SubstanceType.Beer;

            _water = ScriptableObject.CreateInstance<Substance>();
            _water.Color = Color.yellow;
            _water.MillilitersPerMole = 35;
            _water.Type = SubstanceType.Water;
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
       
            _containerLocked.AddSubstance(_beer, int.MaxValue);
            float currentVolume = _containerLocked.CurrentVolume;
            Assert.AreEqual(currentVolume, 0);
        }

        [Test]
        public void RemoveCorrectAmountOfEachSubstanceWhenRemovingMoles()
        {
            _container.AddSubstance(_beer, 10);
            _container.AddSubstance(_water, 5);
            _container.RemoveMoles(1.5f);
            float waterMole = _container.Substances.FirstOrDefault(x => x.Substance.Type == _water.Type).Moles;
            float beerMole = _container.Substances.FirstOrDefault(x => x.Substance.Type == _beer.Type).Moles;
            Assert.AreEqual(9f, beerMole);
            Assert.AreEqual(4.5f, waterMole);
        }
    }
}
