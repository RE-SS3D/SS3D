using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Unique, persistent object that the player owns. Server purpose of holding relevant data of a specific player.
    /// </summary>
    public sealed class Player : NetworkActor
    {
        [SyncVar(OnChange = nameof(SyncCkey))]
        private string _ckey = string.Empty;

        /// <summary>
        /// Unique client key, originally used in BYOND's user management, nostalgically used.
        /// </summary>
        public string Ckey => _ckey;

        /// <summary>
        /// This the owner of this object is the local connection.
        /// </summary>
        public bool IsLocalConnection => Owner == LocalConnection;

        [Server]
        public void SetCkey(string ckey)
        {
            _ckey = ckey;
        }

        /// <summary>
        /// Used by FishNet Networking to update the variable and sync it across instances.
        /// This is also called by the server when the client enters the server to update his data.
        /// </summary>
        public void SyncCkey(string oldCkey, string newCkey, bool asServer)
        {
            gameObject.name = "Player - " + _ckey;
        }
    }
}
