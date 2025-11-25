using FishNet.Object;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Systems.Entities;
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
        private Brain _brain;

        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnOrgans();
            StartCoroutine(AddInternalOrgans());
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
            GetComponentInParent<Entity>().gameObject.SetActive(false);
        }

        protected override void AfterSpawningCopiedBodyPart()
        {
            Entity entityControllingHead = GetComponentInParent<Entity>();
            if (entityControllingHead)
            {
                entityControllingHead.DeactivateComponents();
            }

            // When detached, spawn a head and set player's mind to be in the head,
            // so that player can still play as a head (death is near though..).
            MindSubSystem mindSubSystem = Subsystems.Get<MindSubSystem>();

            if (entityControllingHead && entityControllingHead.Mind != null)
            {
                mindSubSystem.SwapMinds(GetComponentInParent<Entity>(), SpawnedCopy.GetComponent<Entity>());
                SpawnedCopy.GetComponent<NetworkObject>().RemoveOwnership();

                EntitySubSystem entitySubSystem = Subsystems.Get<EntitySubSystem>();
                entitySubSystem.TryTransferEntity(GetComponentInParent<Entity>(), SpawnedCopy.GetComponent<Entity>());
            }
        }

        protected override void BeforeDestroyingBodyPart()
        {
            GetComponentInParent<Entity>().DeactivateComponents();
        }

        protected override void SpawnOrgans()
        {
            GameObject brainPrefab = Items.HumanBrain;
            GameObject brainGameObject = Instantiate(brainPrefab);
            _brain = brainGameObject.GetComponent<Brain>();
            _brain.HealthController = HealthController;
            Spawn(brainGameObject, Owner);
        }

        /// <summary>
        /// Add specific torso internal organs, heart, lungs, and more to come..
        /// Need to do it with a delay to prevent some Unity bug since OnStartServer() is called Before Start();
        /// </summary>
        private IEnumerator AddInternalOrgans()
        {
            yield return null;
            AddInternalBodyPart(_brain);
        }
    }
}
