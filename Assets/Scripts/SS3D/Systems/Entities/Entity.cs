﻿using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Events;
using UnityEngine;
using UnityEngine.Serialization;

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
        private Mind mind = Entities.Mind.Empty;

        public Mind Mind
        {
            get => mind;
            set => mind = value;
        }

        public string Ckey => mind.player.Ckey;

        protected override void OnStart()
        {
            base.OnStart();

            OnSpawn();
        }

        private void OnSpawn()
        {
            OnMindChanged?.Invoke(Mind);
        }

        private void InvokeLocalPlayerObjectChanged()
        {
            if (Mind == null) return;

            if (!Mind.player.IsLocalConnection)
            {
                return;
            }

            LocalPlayerObjectChanged localPlayerObjectChanged = new(GameObject);
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

            OnMindChanged?.Invoke(mind);
            InvokeLocalPlayerObjectChanged();
        }

        /// <summary>
        /// Updates the mind of this entity.
        /// </summary>
        /// <param name="mind">The new mind.</param>
        [Server]
        public void SetMind(Mind mind)
        {
            this.mind = mind;

            GiveOwnership(mind.Owner);
        }
    }
}