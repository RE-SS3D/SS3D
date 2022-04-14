using System;
using Mirror;
using UnityEngine;

namespace SS3D.Core.Networking.PlayerControl.Messages
{
    /// <summary>
    /// Fired when a user authenticates
    /// </summary>
    [Serializable]
    public struct UserJoinedServerMessage : NetworkMessage
    {
        public readonly string Ckey;

        public UserJoinedServerMessage(string ckey)
        {
            Ckey = ckey;
        }
    }
}
