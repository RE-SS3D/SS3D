using System;
using FishNet.Broadcast;

namespace SS3D.Systems.PlayerControl.Messages
{
    /// <summary>
    /// Network messaged used to authorize the client.
    /// TODO: Update this with actual access token and add validation in PlayerControlManager later
    /// </summary>
    [Serializable]
    public struct UserAuthorizationMessage : IBroadcast
    {
        public readonly string Ckey;

        /// <summary>Optional admin secret from <c>-token=</c>; see <see cref="SS3D.Permissions.AdminTokens"/>.</summary>
        public readonly string Token;

        public UserAuthorizationMessage(string ckey, string token = null)
        {
            Ckey = ckey;
            Token = token;
        }
    }
}