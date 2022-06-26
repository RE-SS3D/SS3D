using Coimbra.Services.Events;
using FishNet.Broadcast;

namespace SS3D.Core.Networking.Lobby.Messages
{
    public struct UserLeftLobbyMessage : IBroadcast
    {
        public readonly string Ckey;

        public UserLeftLobbyMessage(string ckey)
        {
            Ckey = ckey;
        }
    }
}