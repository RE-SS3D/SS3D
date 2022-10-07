using FishNet.Connection;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Base class for all things that can be controlled by a player
    /// </summary>
    public class PlayerControllable : NetworkedSpessBehaviour
    {
        [SyncVar(OnChange = "SyncControllingSoul")] [SerializeField] private NetworkConnection _controllingSoul;

        protected bool ControlledByLocalPlayer => _controllingSoul == LocalConnection;

        public NetworkConnection ControllingSoul
        {
            get => _controllingSoul;
            set => _controllingSoul = value;
        }

        private void SyncControllingSoul(NetworkConnection oldOwner, NetworkConnection newOwner, bool asServer)
        {
            _controllingSoul = newOwner;
        }
    }
}