﻿using FishNet.Object;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Health;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using Coimbra;
using SS3D.Core;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Base class for all humans
    /// </summary>
    public class Human : Entity
    {
        // prefab for the ghost 
		public GameObject Ghost;
		private GameObject _spawnedGhost;


		/// <summary>
		/// On death, the player should become a ghost.
		/// </summary>
		[Server]
		private void BecomeGhost(GameObject player, GameObject ghost)
		{
			Entity originEntity = player.GetComponent<Entity>();
			Entity ghostEntity = ghost.GetComponent<Entity>();

            MindSystem mindSystem = Subsystems.Get<MindSystem>();
            mindSystem.SwapMinds(originEntity, ghostEntity);

            RpcUpdateGhostPosition(originEntity, ghostEntity);
            RpcDestroyObjects(originEntity);
			
		}


        /// <summary>
        /// This method should probably be called turn in corpse, and instead of destroying components it should deactivate them.
        /// Should also trigger the ragdoll.
        /// </summary>
		[ObserversRpc(RunLocally = true)]
		private void RpcDestroyObjects(Entity originEntity)
		{
			GameObject originEntityGameObject = originEntity.gameObject;
			originEntityGameObject.GetComponent<Hands>()?.Dispose(true);
			originEntityGameObject.GetComponent<HumanInventory>()?.Dispose(true);
			originEntityGameObject.GetComponent<InteractionController>()?.Dispose(true);
			originEntityGameObject.GetComponent<StaminaController>()?.Dispose(true);
			originEntityGameObject.GetComponent<HumanoidController>()?.Dispose(true);
            originEntityGameObject.GetComponent<HumanoidLivingController>()?.Dispose(true);
            // TODO: Optimize these GetComponents, this is a temporary solution.
        }

		/// <summary>
		/// Put Ghost at the same place as the deceased player.
		/// </summary>
		[ObserversRpc(RunLocally = true)]
		private void RpcUpdateGhostPosition(Entity originEntity, Entity ghostEntity)
		{
			ghostEntity.Transform.SetPositionAndRotation(originEntity.Transform.position, originEntity.Transform.rotation);
            // Little rotation to have the ghost starting on the floor. Can be improved.
			originEntity.Transform.Rotate(new Vector3(90, 0, 0));
		}

		/// <summary>
		/// Kill a player, instantiating a ghost.
		/// </summary>
		[Server]
		public override void Kill()
		{
			_spawnedGhost = Instantiate(Ghost);
			var entitySystem = Subsystems.Get<EntitySystem>();
			if(entitySystem.TryTransferEntity(GetComponentInParent<Entity>(), _spawnedGhost.GetComponent<Entity>()))
            {
                ServerManager.Spawn(_spawnedGhost);
                BecomeGhost(gameObject, _spawnedGhost);
            }
            else
            {
                _spawnedGhost.Dispose(true);
            }
		}

        public override void DeactivateComponents()
        {
            RpcDestroyObjects(this);
        }
    }
}