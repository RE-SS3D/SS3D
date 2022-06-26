using System;
using SS3D.Core.Systems.Lobby;
using SS3D.Core.Systems.Rounds;
using Object = UnityEngine.Object;

namespace SS3D.Core
{
    public static class GameSystems
    {
        private static LobbySystem _lobbySystem;
        private static RoundSystem _roundSystem;

        public static LobbySystem LobbySystem
        {
            get
            {
                if (_lobbySystem != null) { return _lobbySystem; }

                _lobbySystem = Object.FindObjectOfType<LobbySystem>();
                if (_lobbySystem == null)
                {
                    throw new Exception($"[{nameof(GameSystems)}] - Couldn't find lobby system");
                }

                return _lobbySystem;
            }
        }

        public static RoundSystem RoundSystem
        {
            get
            {
                if (_roundSystem != null) { return _roundSystem; }

                _roundSystem = Object.FindObjectOfType<RoundSystem>();
                if (_roundSystem == null)
                {
                    throw new Exception($"[{nameof(GameSystems)}] - Couldn't find round system");
                }

                return _roundSystem;
            }
        }
    }
}