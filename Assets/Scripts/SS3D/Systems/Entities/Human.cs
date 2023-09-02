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

			Mind originMind = originEntity.Mind;

			ghostEntity.SetMind(originMind);
			RpcDestroyObjects(originEntity);
			RpcUpdateGhostPosition(originEntity, ghostEntity);
		}

		[ObserversRpc]
		private void RpcDestroyObjects(Entity originEntity)
		{
			GameObject originEntityGameObject = originEntity.gameObject;
			originEntityGameObject.GetComponent<Hands>()?.Dispose(true);
			originEntityGameObject.GetComponent<HumanInventory>()?.Dispose(true);
			originEntityGameObject.GetComponent<InteractionController>()?.Dispose(true);
			originEntityGameObject.GetComponent<StaminaController>()?.Dispose(true);
			originEntityGameObject.GetComponent<HumanoidController>()?.Dispose(true);
			// TODO: Optimize these GetComponents, this is a temporary solution.
		}

		/// <summary>
		/// Put Ghost at the same place as the deceased player.
		/// </summary>
		[ObserversRpc]
		private void RpcUpdateGhostPosition(Entity originEntity, Entity ghostEntity)
		{
			ghostEntity.Transform.SetPositionAndRotation(originEntity.Transform.position, originEntity.Transform.rotation);
			originEntity.Transform.Rotate(new Vector3(90, 0, 0));
		}

		/// <summary>
		/// Kill a player, instantiating a ghost.
		/// </summary>
		[Server]
		public override void Kill()
		{
			_spawnedGhost = Instantiate(Ghost);
			ServerManager.Spawn(_spawnedGhost);
			var entitySystem = Subsystems.Get<EntitySystem>();
			entitySystem.TransferEntity(GetComponentInParent<Entity>(), _spawnedGhost.GetComponent<Entity>());
			BecomeGhost(gameObject, _spawnedGhost);
		}
	}
}