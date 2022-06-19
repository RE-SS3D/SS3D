using Coimbra.Services.Events;

namespace SS3D.Core.Networking.Lobby.Events
{
    public partial struct UserLeftLobbyEvent : IEvent
    {
        public readonly string Ckey;

        public UserLeftLobbyEvent(string ckey)
        {
            Ckey = ckey;
        }
    }
}