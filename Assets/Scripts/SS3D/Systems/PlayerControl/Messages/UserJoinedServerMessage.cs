using System;
using FishNet.Broadcast;

namespace SS3D.Systems.PlayerControl.Messages
{
    /// <summary>
    /// Fired when a user authenticates
    /// </summary>
    [Serializable]
    public struct UserJoinedServerMessage : IBroadcast
    {
        public readonly string Ckey;

        public UserJoinedServerMessage(string ckey)
        {
            Ckey = ckey;
        }
    }
}
