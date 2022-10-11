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
    public class PlayerControllable : NetworkedSpessBehaviour
    {
        public Action<Soul> ControllingSoulChanged;

        [SyncVar(OnChange = "SetControllingSoul")] private Soul _controllingSoul;

        [SerializeField] private bool _scaleInOnSpawn = true;

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
            if (_scaleInOnSpawn)
            {
                LocalScale = Vector3.zero;
                TransformCache.DOScale(1, ScaleInDuration).SetEase(Ease.OutCirc);
            }

            ControllingSoulChanged?.Invoke(ControllingSoul);
        }

        [Server]
        public void ProcessDespawn()
        {
            TransformCache.DOScale(0, ScaleInDuration).SetEase(Ease.OutElastic).OnComplete(() => ServerManager.Despawn(GameObjectCache));
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            UpdateCameraFollow();
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

        public void SetControllingSoul(Soul oldSoul, Soul newSoul, bool asServer)
        {
            _controllingSoul = newSoul;

            ControllingSoulChanged?.Invoke(_controllingSoul);
            UpdateCameraFollow();
        }

        [Server]
        public void ChangeControllingSoul(Soul soul)
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

            ControllingSoulChanged?.Invoke(_controllingSoul);
            UpdateCameraFollow();
        }
    }
}