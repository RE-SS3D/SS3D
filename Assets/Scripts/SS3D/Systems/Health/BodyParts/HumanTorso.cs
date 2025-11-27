using SS3D.Data.Generated;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using System.Collections;
using UnityEngine;

namespace SS3D.Systems.Health
{
    public class HumanTorso : BodyPart
    {
        private Heart _heart;
        private Lungs _leftLung;
        private Lungs _rightLung;

        protected override bool IsDetachable => false;

        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnOrgans();
            StartCoroutine(AddInternalOrgans());
        }

        protected override void SpawnOrgans()
        {
            GameObject heartPrefab = Items.HumanHeart;
            GameObject leftLungPrefab = Items.HumanLungLeft;
            GameObject rightLungPrefab = Items.HumanLungRight;

            GameObject heartGameObject = Instantiate(heartPrefab);
            GameObject leftLungGameObject = Instantiate(leftLungPrefab);
            GameObject rightLungGameObject = Instantiate(rightLungPrefab);

            _heart = heartGameObject.GetComponent<Heart>();
            _leftLung = leftLungGameObject.GetComponent<Lungs>();
            _rightLung = rightLungGameObject.GetComponent<Lungs>();

            Spawn(heartGameObject, Owner);
            Spawn(leftLungGameObject, Owner);
            Spawn(rightLungGameObject, Owner);
        }

        protected override void AddInitialLayers()
        {
            TryAddBodyLayer(new MuscleLayer(this));
            TryAddBodyLayer(new BoneLayer(this));
            TryAddBodyLayer(new CirculatoryLayer(this, 8f));
            TryAddBodyLayer(new NerveLayer(this));

            InvokeOnBodyPartLayerAdded();
        }

        protected override void AfterSpawningCopiedBodyPart() { }

        protected override void BeforeDestroyingBodyPart() { }

        /// <summary>
        /// Add specific torso internal organs, heart, lungs, and more to come..
        /// Need to do it with a delay to prevent some Unity bug since OnStartServer() is called Before Start();
        /// </summary>
        private IEnumerator AddInternalOrgans()
        {
            yield return null;
            AddInternalBodyPart(_heart);
            AddInternalBodyPart(_leftLung);
            AddInternalBodyPart(_rightLung);
        }
    }
}
