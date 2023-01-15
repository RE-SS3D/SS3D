using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Screens.Events;
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
        private Mind _mind;

        public Mind Mind
        {
            get => _mind;
            set => _mind = value;
        }

        public string Ckey => _mind.Soul.Ckey;

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
            if (!Mind.Soul.IsLocalConnection)
            {
                return;
            }

            LocalPlayerObjectChanged localPlayerObjectChanged = new(GameObjectCache);
            localPlayerObjectChanged.Invoke(this);
        }

        public void SyncMind(Mind oldMind, Mind newSoul, bool asServer)
        {
            if (!asServer && IsHost)
            {
                return;
            }

            OnMindChanged?.Invoke(_mind);
            InvokeLocalPlayerObjectChanged();
        }

        [Server]
        public void SetMind(Mind mind)
        {
            _mind = mind;

            GiveOwnership(mind.Owner);
        }
    }
}