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
        [SyncVar(OnChange = "SyncControllingSoul")] private NetworkConnection _controllingSoul;

        protected bool ControlledByLocalPlayer => _controllingSoul == LocalConnection;

        public NetworkConnection ControllingSoul
        {
            get => _controllingSoul;
            set => SetControllingSoul(value);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            SetCameraFollow();
        }

        private void SyncControllingSoul(NetworkConnection oldOwner, NetworkConnection newOwner, bool asServer)
        {
            _controllingSoul = newOwner;

            SetCameraFollow();
        }

        private void SetControllingSoul(NetworkConnection controllingSoul)
        {
            _controllingSoul = controllingSoul;

            SetCameraFollow();
        }

        private void SetCameraFollow()
        {
            if (_controllingSoul != LocalConnection)
            {
                return;
            }

            ChangeCameraEvent changeCameraEvent = new(GameObjectCache);
            changeCameraEvent.Invoke(this);
        }
    }
}