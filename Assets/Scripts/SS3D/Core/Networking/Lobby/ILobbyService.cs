using System.Collections.Generic;
using Coimbra.Services;

namespace SS3D.Core.Networking.Lobby
{
    public interface ILobbyService : IService
    {
        List<string> CurrentLobbyPlayers();
    }
}