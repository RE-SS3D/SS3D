using Coimbra.Services.Events;

namespace SS3D.Core.Networking.Lobby.Events
{
    public partial struct UserJoinedLobbyEvent : IEvent
    {
        public readonly string Ckey;

        public UserJoinedLobbyEvent(string ckey)
        {
            Ckey = ckey;
        }
    }
}