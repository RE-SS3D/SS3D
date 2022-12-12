using System;
using DG.Tweening;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Systems.Screens.Events;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Base class for all things that can be controlled by a player
    /// </summary>
    public class PlayerControllable : NetworkActor
    {
        public Action<Soul> ControllingSoulChanged;

        [SyncVar(OnChange = "SyncControllingSoul")] private Soul _controllingSoul;

        public Soul ControllingSoul
        {
            get => _controllingSoul;
            set => _controllingSoul = value;
        }

        private const float ScaleInDuration = .6f;

        protected override void OnStart()
        {
            base.OnStart();

            OnSpawn();
        }

        private void OnSpawn()
        {
            ControllingSoulChanged?.Invoke(ControllingSoul);
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

        public void SyncControllingSoul(Soul oldSoul, Soul newSoul, bool asServer)
        {
            _controllingSoul = newSoul;

            ControllingSoulChanged?.Invoke(_controllingSoul);
            UpdateCameraFollow();
        }

        [Server]
        public void SetControllingSoul(Soul soul)
        {
            _controllingSoul = soul;

            if (soul == null)
            {
                RemoveOwnership();
            }
            else
            {
                GiveOwnership(soul.Owner);   
            }
        }
    }
}