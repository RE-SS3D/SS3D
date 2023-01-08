using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
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
        public Action<Mind> MindChanged;

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
            MindChanged?.Invoke(Mind);
        }

        private void UpdateCameraFollow()
        {
            if (!IsOwner)
            {
                return;
            }

            ChangeCameraEvent changeCameraEvent = new(GameObjectCache);
            changeCameraEvent.Invoke(this);
        }

        public void SyncMind(Mind oldMind, Mind newSoul, bool asServer)
        {
            _mind = newSoul;

            MindChanged?.Invoke(_mind);
            UpdateCameraFollow();
        }

        [Server]
        public void SetMind(Mind mind)
        {
            _mind = mind;

            if (mind == null)
            {
                RemoveOwnership();
            }
            else
            {
                GiveOwnership(mind.Owner);
            }
        }
    }
}