using System;
using DG.Tweening;
using FishNet.Connection;
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
        public Action<NetworkConnection> OnOwnerChanged;

        [SerializeField] private bool _scaleInOnSpawn = true;
        private const float ScaleInDuration = .6f;

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            OnOwnerChanged?.Invoke(Owner);

            SetCameraFollow();
        }

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
                TransformCache.DOScale(1, ScaleInDuration).SetEase(Ease.OutElastic);
            }

            OnOwnerChanged?.Invoke(Owner);
        }

        public void ProcessDespawn()
        {
            TransformCache.DOScale(0, ScaleInDuration).SetEase(Ease.OutElastic).OnComplete(() => ServerManager.Despawn(GameObjectCache));
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            SetCameraFollow();
        }

        private void SetCameraFollow()
        {
            if (!IsOwner)
            {
                return;
            }

            ChangeCameraEvent changeCameraEvent = new(GameObjectCache);
            changeCameraEvent.Invoke(this);
        }

        public void SetOwner(NetworkConnection conn)
        {
            NetworkObject.RemoveOwnership();
            NetworkObject.GiveOwnership(conn);

            OnOwnerChanged?.Invoke(conn);
        }
    }
}