using FishNet.Broadcast;

namespace SS3D.Systems.Lobby.Messages
{
    public struct UserJoinedLobbyMessage : IBroadcast
    {
        public readonly string Ckey;

        public UserJoinedLobbyMessage(string ckey)
        {
            Ckey = ckey;
        }
    }
}