using System;
using FishNet.Broadcast;

namespace SS3D.Systems.PlayerControl.Messages
{
    /// <summary>
    /// Network messaged used to authorize the client.
    /// TODO: Update this with actual access token and add validation in PlayerControlManager later
    /// </summary>
    [Serializable]
    public struct UserLeftServerMessage : IBroadcast
    {
        public readonly string Ckey;

        public UserLeftServerMessage(string ckey)
        {
            Ckey = ckey;
        }
    }
}