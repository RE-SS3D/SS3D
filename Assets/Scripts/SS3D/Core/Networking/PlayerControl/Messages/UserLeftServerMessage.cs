using System;
using Mirror;

namespace SS3D.Core.Networking.PlayerControl.Messages
{
    /// <summary>
    /// Network messaged used to authorize the client.
    /// TODO: Update this with actual access token and add validation in PlayerControlManager later
    /// </summary>
    [Serializable]
    public struct UserLeftServerMessage : NetworkMessage
    {
        public readonly string Ckey;

        public UserLeftServerMessage(string ckey)
        {
            Ckey = ckey;
        }
    }
}