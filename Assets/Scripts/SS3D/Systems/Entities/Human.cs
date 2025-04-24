using FishNet.Object;
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

            MindSubSystem mindSystem = SubSystems.Get<MindSubSystem>();
            mindSystem.SwapMinds(originEntity, ghostEntity);

            RpcUpdateGhostPosition(originEntity, ghostEntity);
            if (TryGetComponent(out Ragdoll ragdoll))
            {
                ragdoll.KnockdownTimeless();
            }
            RpcDestroyComponents(originEntity);
        }

        /// <summary>
        /// Destroys all "human" components, such as Hands and HumanoidController. Also activates ragdoll
        /// </summary>
		[ObserversRpc(RunLocally = true)]
		private void RpcDestroyComponents(Entity originEntity)
		{
            // Instead of destroying components it should deactivate them.
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
        }

		/// <summary>
		/// Kill a player, instantiating a ghost.
		/// </summary>
		[Server]
		public override void Kill()
		{
            _spawnedGhost = Instantiate(Ghost);
			EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
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
            RpcDestroyComponents(this);
        }
    }
}