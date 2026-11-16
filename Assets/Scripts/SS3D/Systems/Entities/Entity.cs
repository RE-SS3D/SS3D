using System;
using Coimbra;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Networking;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Base class for all things that can be controlled by a player.
    /// </summary>
    [Serializable]
    public class Entity : NetworkActor
    {
        /// <summary>
        /// A reference point to use for this Entities perspective
        /// </summary>
        public GameObject ViewPoint;
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

        private HumanInventory _cachedHumanInventory;

        /// <summary>Cached inventory lookup for hot paths (airlock proximity, etc.).</summary>
        public bool TryGetHumanInventory(out HumanInventory inventory)
        {
            if (_cachedHumanInventory == null)
                TryGetComponent(out _cachedHumanInventory);

            inventory = _cachedHumanInventory;
            return inventory != null;
        }

        private const float ObserverGridCheckIntervalSeconds = 0.25f;

        private Vector2Int? _lastObserverGridCell;
        private float _nextObserverGridCheckTime;

        protected override void OnStart()
        {
            base.OnStart();

            OnSpawn();
        }

        private void Update()
        {
            if (!IsServer)
                return;

            NetworkConnection owner = Owner;
            if (!owner.IsValid || owner.FirstObject != NetworkObject)
                return;

            float now = Time.unscaledTime;
            if (now < _nextObserverGridCheckTime)
                return;

            _nextObserverGridCheckTime = now + ObserverGridCheckIntervalSeconds;

            Vector2Int gridCell = TileObserverConstants.GetHashGridCell(transform.position);
            if (_lastObserverGridCell == gridCell)
                return;

            _lastObserverGridCell = gridCell;
            ServerManager.Objects.RebuildObservers(owner, timedOnly: false);
        }

        private void OnSpawn()
        {
            OnMindChanged?.Invoke(Mind);
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);

            // Reclaim restores ownership without changing Mind SyncVar — HUD listens to this
            // event, but lobby used to wait only on SpawnedPlayersUpdated (which does not re-fire).
            if (IsOwner)
            {
                InvokeLocalPlayerObjectChanged();
            }
            else if (prevOwner != null && prevOwner == LocalConnection)
            {
                new LocalPlayerObjectChanged(GameObject, false).Invoke(this);
            }
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
            bool isLocal = IsOwner
                || (Mind?.player != null && Mind.player.IsLocalConnection);
            if (!isLocal)
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
            _mind = mind;
            // Ghosts ship with a null mind; SwapMinds assigns that to the corpse. Must clear FishNet
            // ownership or the dead player keeps Owner and still receives corpse TargetRpcs (hit flash).
            if (mind == null || mind == Mind.Empty)
            {
                RemoveOwnership();
                return;
            }

            GiveOwnership(mind.Owner);
        }

		public virtual void Kill()
		{
			throw new NotImplementedException();
		}

        public virtual void DeactivateComponents()
        {
            throw new NotImplementedException();
        }
    }
}