using FishNet.Object;
using SS3D.Core;
using SS3D.Data.Enums;
using SS3D.Data;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Items;
using System.Collections;
using UnityEngine;

namespace SS3D.Systems.Health
{
	/// <summary>
	/// Body part for a human head.
	/// </summary>
	public class HeadBodyPart : BodyPart
	{
		public Brain brain;

		public override void Init(BodyPart parent)
		{
			base.Init(parent);
		}

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
            AddInternalBodyPart(brain);
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

            GetComponentInParent<Human>()?.DeactivateComponents();

            // When detached, spawn a head and set player's mind to be in the head,
            // so that player can still play as a head (death is near though..).
            MindSystem mindSystem = Subsystems.Get<MindSystem>();

            var EntityControllingHead = GetComponentInParent<Entity>();
            if (EntityControllingHead.Mind != null)
            {
                mindSystem.SwapMinds(GetComponentInParent<Entity>(), _spawnedCopy.GetComponent<Entity>());
                _spawnedCopy.GetComponent<NetworkObject>().RemoveOwnership();

                EntitySystem entitySystem = Subsystems.Get<EntitySystem>();
                entitySystem.TryTransferEntity(GetComponentInParent<Entity>(), _spawnedCopy.GetComponent<Entity>());
            }
        }

        protected override void BeforeDestroyingBodyPart()
        {
            GetComponentInParent<Human>()?.DeactivateComponents();
            return;
        }

        protected override void SpawnOrgans()
        {
            GameObject brainPrefab = Assets.Get<GameObject>((int)AssetDatabases.Items, (int)ItemId.HumanBrain);
            GameObject brainGameObject = Instantiate(brainPrefab);
            brain = brainGameObject.GetComponent<Brain>();

            brain.HealthController = HealthController;

            Spawn(brainGameObject, Owner);
        }
    }
}
