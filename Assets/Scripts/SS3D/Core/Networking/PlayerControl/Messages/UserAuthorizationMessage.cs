using System;
using Mirror;

namespace SS3D.Core.Networking.PlayerControl.Messages
{
    /// <summary>
    /// Network messaged used to authorize the client.
    /// TODO: Update this with actual access token and add validation in PlayerControlManager later
    /// </summary>
    [Serializable]
    public struct UserAuthorizationMessage : NetworkMessage
    {
        public readonly string Ckey;

        public  UserAuthorizationMessage(string ckey)
        {
            Ckey = ckey;
        }
    }
}