using System;
using SS3D.Core.Networking.Lobby;
using Object = UnityEngine.Object;

namespace SS3D.Core
{
    public static class GameSystems
    {
        private static LobbySystem _lobbySystem;

        public static LobbySystem LobbySystem
        {
            get
            {
                if (_lobbySystem == null && Object.FindObjectOfType<LobbySystem>())
                {
                    _lobbySystem = Object.FindObjectOfType<LobbySystem>();       
                }
                else
                {
                    throw new Exception($"[{nameof(GameSystems)}] - Couldn't find lobby system");
                }

                return _lobbySystem;
            }
        }
    }
}