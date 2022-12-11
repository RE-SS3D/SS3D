using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Logging;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Unique, persistent object that the player owns, it manages what character it is controlling and stores other player data.
    /// </summary>
    [Serializable]
    public sealed class Soul : NetworkActor
    {
        [SyncVar(OnChange = nameof(UpdateCkey))] private string _ckey;

        /// <summary>
        /// Unique client key, originally used in BYOND's user management, nostalgically used.
        /// </summary>
        public string Ckey => _ckey;

        /// <summary>
        /// Used by FishNet Networking to update the variable and sync it across instances.
        /// This is also called by the server when the client enters the server to update his data.
        /// </summary>
        public void UpdateCkey(string oldCkey, string newCkey, bool asServer)
        {
            if (oldCkey != newCkey)
            {
                Punpun.Say(this, $"Updating player ckey {newCkey}");
            }

            _ckey = newCkey; 
            gameObject.name = "Soul: " + _ckey;
        }
    }
}
