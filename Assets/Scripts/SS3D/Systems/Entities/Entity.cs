using System;
using Coimbra;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Health;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Base class for all things that can be controlled by a player.
    /// </summary>
    [Serializable]
    public class Entity : NetworkActor
    {
        public event Action<Mind> OnMindChanged;

        [SerializeField]
        [SyncVar(OnChange = nameof(SyncMind))]
        private Mind _mind = Mind.Empty;

        public Mind Mind
        {
            get => _mind;
            set => _mind = value;
        }

        public string Ckey => _mind.player.Ckey;

        protected override void OnStart()
        {
            base.OnStart();

            OnSpawn();
        }

        private void OnSpawn()
        {
            OnMindChanged?.Invoke(Mind);
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            if (IsOwner)
            {
                LocalPlayerObjectChanged localPlayerObjectChanged = new(GameObject, false);
                localPlayerObjectChanged.Invoke(this);
            }
        }

        private void InvokeLocalPlayerObjectChanged()
        {
            if (Mind == null) return;

            if (!Mind.player.IsLocalConnection)
            {
                return;
            }

            LocalPlayerObjectChanged localPlayerObjectChanged = new(GameObject, true);
            localPlayerObjectChanged.Invoke(this);
        }

        /// <summary>
        /// Called by FishNet when the value of _mind is synced.
        /// </summary>
        /// <param name="oldMind">Value before sync</param>
        /// <param name="newMind">Value after sync</param>
        /// <param name="asServer">Is the sync is being called as the server (host and server only)</param>
        public void SyncMind(Mind oldMind, Mind newMind, bool asServer)
        {
            if (!asServer && IsHost)
            {
                return;
            }

            OnMindChanged?.Invoke(_mind);
            InvokeLocalPlayerObjectChanged();
        }

        /// <summary>
        /// Updates the mind of this entity.
        /// </summary>
        /// <param name="mind">The new mind.</param>
        [Server]
        public void SetMind(Mind mind)
        {
            this._mind = mind;
            if(mind == null) return;
            GiveOwnership(mind.Owner);
        }

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
        public void Kill()
        {
            _spawnedGhost = Instantiate(Ghost);
            ServerManager.Spawn(_spawnedGhost);
            BecomeGhost(gameObject, _spawnedGhost);
        }
    }
}