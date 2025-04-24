using FishNet.Object;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Health
{
	/// <summary>
	/// Body part for a human head.
	/// </summary>
	public class HeadBodyPart : BodyPart
	{
		[FormerlySerializedAs("brain")]
        public Brain Brain;

        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnOrgans();
            StartCoroutine(AddInternalOrgans());
        }

        /// <summary>
        /// Add specific torso internal organs, heart, lungs, and more to come..
        /// Need to do it with a delay to prevent some Unity bug since OnStartServer() is called Before Start();
        /// </summary>
        private IEnumerator AddInternalOrgans()
        {
            yield return null;
            AddInternalBodyPart(Brain);
        }

        protected override void AddInitialLayers()
		{
			TryAddBodyLayer(new MuscleLayer(this));
			TryAddBodyLayer(new BoneLayer(this));
			TryAddBodyLayer(new CirculatoryLayer(this, 5f));
			TryAddBodyLayer(new NerveLayer(this));
			InvokeOnBodyPartLayerAdded();
		}

        /// <summary>
        /// Deactivate this game object, should run for all observers, and for late joining (hence bufferlast = true).
        /// </summary>
        [ObserversRpc(RunLocally = true, BufferLast = true)]
		protected void DeactivateWholeBody()
		{
			GetComponentInParent<Human>().gameObject.SetActive(false);
		}

        protected override void AfterSpawningCopiedBodyPart()
        {
            GetComponentInParent<Ragdoll>().KnockdownTimeless();
            GetComponentInParent<Human>()?.DeactivateComponents();

            // When detached, spawn a head and set player's mind to be in the head,
            // so that player can still play as a head (death is near though..).
            MindSubSystem mindSystem = SubSystems.Get<MindSubSystem>();

            Entity entityControllingHead = GetComponentInParent<Entity>();
            if (entityControllingHead.Mind != null)
            {
                mindSystem.SwapMinds(GetComponentInParent<Entity>(), _spawnedCopy.GetComponent<Entity>());
                _spawnedCopy.GetComponent<NetworkObject>().RemoveOwnership();

                EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
                entitySystem.TryTransferEntity(GetComponentInParent<Entity>(), _spawnedCopy.GetComponent<Entity>());
            }
        }

        protected override void BeforeDestroyingBodyPart()
        {
            GetComponentInParent<Human>()?.DeactivateComponents();
        }

        protected override void SpawnOrgans()
        {
            GameObject brainPrefab = Items.HumanBrain;
            GameObject brainGameObject = Instantiate(brainPrefab);
            Brain = brainGameObject.GetComponent<Brain>();
            Brain.HealthController = HealthController;
            Spawn(brainGameObject, Owner);
        }
    }
}
