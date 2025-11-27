using Coimbra;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Health;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Base class for all humans
    /// </summary>
    public class Human : Entity, IRagdollable
    {
        // prefab for the ghost
        [FormerlySerializedAs("Ghost")]
        [SerializeField]
        private GameObject _ghost;

        private GameObject _spawnedGhost;

        /// <summary>
        /// Kill a player, instantiating a ghost.
        /// </summary>
        [Server]
        public override void Kill()
        {
            _spawnedGhost = Instantiate(_ghost);
            EntitySubSystem entitySubSystem = Subsystems.Get<EntitySubSystem>();

            if (entitySubSystem.TryTransferEntity(GetComponentInParent<Entity>(), _spawnedGhost.GetComponent<Entity>()))
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

        public void Recover() => GetComponent<Ragdoll>().Recover();

        public void AddForceToAllRagdollParts(Vector3 vector3) => GetComponent<Ragdoll>().AddForceToAllParts(vector3);

        public void Knockdown(float time) => GetComponent<Ragdoll>().KnockDown(time);

        public bool IsRagdolled() => GetComponent<PositionController>().PositionType == PositionType.Ragdoll;

        /// <summary>
        /// On death, the player should become a ghost.
        /// </summary>
        [Server]
        private void BecomeGhost(GameObject player, GameObject ghost)
        {
            Entity originEntity = player.GetComponent<Entity>();
            Entity ghostEntity = ghost.GetComponent<Entity>();

            MindSubSystem mindSubSystem = Subsystems.Get<MindSubSystem>();
            mindSubSystem.SwapMinds(originEntity, ghostEntity);

            RpcUpdateGhostPosition(originEntity, ghostEntity);

            if (TryGetComponent(out Ragdoll ragdoll))
            {
                ragdoll.KnockDown();
            }

            RpcDestroyComponents(originEntity);
        }

        /// <summary>
        /// Disablle all "human" components, such as Hands and HumanoidController. Also activates ragdoll
        /// </summary>
        [ObserversRpc(RunLocally = true)]
        private void RpcDestroyComponents(Entity originEntity)
        {
            GameObject originEntityGameObject = originEntity.gameObject;
            originEntityGameObject.GetComponent<Hands>().enabled = false;
            originEntityGameObject.GetComponent<HumanInventory>().enabled = false;
            originEntityGameObject.GetComponent<InteractionController>().enabled = false;
            originEntityGameObject.GetComponent<StaminaController>().enabled = false;
            originEntityGameObject.GetComponent<HumanoidMovementController>().enabled = false;
            originEntity.GetComponent<Ragdoll>().KnockDown();
        }

        /// <summary>
        /// Put Ghost at the same place as the deceased player.
        /// </summary>
        [ObserversRpc(RunLocally = true)]
        private void RpcUpdateGhostPosition(Entity originEntity, Entity ghostEntity)
        {
            ghostEntity.Transform.SetPositionAndRotation(originEntity.Transform.position, originEntity.Transform.rotation);
        }
    }
}
