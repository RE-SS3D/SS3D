using DG.Tweening;
using FishNet.Connection;
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
        [SyncVar(OnChange = "SyncControllingSoul")] private Soul _controllingSoul;

        [SerializeField] private bool _scaleInOnSpawn = true;
        private const float ScaleInDuration = .6f;

        protected bool ControlledByLocalPlayer => _controllingSoul.Owner == LocalConnection;

        public Soul ControllingSoul
        {
            get => _controllingSoul;
            set
            {
                _controllingSoul = value;
                SetCameraFollow();
            }
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

        private void SyncControllingSoul(Soul oldControllingSoul, Soul newControllingSoul, bool asServer)
        {
            _controllingSoul = newControllingSoul;

            SetCameraFollow();
        }

        private void SetCameraFollow()
        {
            if (_controllingSoul.Owner != LocalConnection)
            {
                return;
            }

            ChangeCameraEvent changeCameraEvent = new(GameObjectCache);
            changeCameraEvent.Invoke(this);
        }
    }
}