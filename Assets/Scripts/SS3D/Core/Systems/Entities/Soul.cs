using System;
using Coimbra;
using Mirror;
using UnityEngine;

namespace SS3D.Core.Systems.Entities
{
    /// <summary>
    /// Unique, persistent object that the player owns, it manages what character it is controlling and stores other player data.
    /// </summary>
    [Serializable]
    public sealed class Soul : NetworkBehaviour
    {
        [SyncVar(hook = nameof(SetCkey))] private string _ckey;

        /// <summary>
        /// Unique client key, originally used in BYOND's user management, nostalgically used
        /// </summary>
        public string Ckey => _ckey;

        [ClientRpc]
        public void RpcUpdateCkey()
        {
            gameObject.name = "Soul: " + Ckey;
        }
        
        /// <summary>
        /// Used by Mirror Networking to update the variable and sync it across instances.
        /// This is also called by the server when the client enters the server to update his data
        /// </summary>
        public void SetCkey(string oldCkey, string newCkey)
        {
            Debug.Log($"[{typeof(Soul)}] - SyncVarHook - Updating player ckey");
            _ckey = newCkey; 
            gameObject.name = "Soul: " + _ckey;
        }
    }
}
