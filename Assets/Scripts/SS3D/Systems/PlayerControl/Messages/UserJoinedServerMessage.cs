using System;
using FishNet.Broadcast;


namespace SS3D.Core.Networking.PlayerControl.Messages
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
