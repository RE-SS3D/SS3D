using System;
using SS3D.Core.Networking.PlayerControl;
using SS3D.Core.Systems.Lobby;
using SS3D.Core.Systems.Permissions;
using SS3D.Core.Systems.Rounds;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Core
{
    /// <summary>
    /// TODO: Automate this crap
    /// </summary>
    public static class GameSystems
    {
        private static LobbySystem _lobbySystem;
        private static RoundSystem _roundSystem;
        private static PermissionSystem _permissionSystem;
        private static PlayerControlSystem _playerControlSystem;

        public static LobbySystem LobbySystem
        {
            get
            {
                if (_lobbySystem != null) { return _lobbySystem; }

                _lobbySystem = Object.FindObjectOfType<LobbySystem>();
                if (_lobbySystem == null)
                {
                    Debug.Log(new Exception($"[{nameof(GameSystems)}] - Couldn't find lobby system"));
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
                    Debug.Log(new Exception($"[{nameof(GameSystems)}] - Couldn't find round system"));
                }

                return _roundSystem;
            }
        }

        public static PermissionSystem PermissionSystem
        {
            get
            {
                if (_permissionSystem != null) { return _permissionSystem; }

                _permissionSystem = Object.FindObjectOfType<PermissionSystem>();
                if (_permissionSystem == null)
                {
                    Debug.Log(new Exception($"[{nameof(GameSystems)}] - Couldn't find permission system"));
                }

                return _permissionSystem;
            }
        }

        public static PlayerControlSystem PlayerControlSystem
        {
            get
            {
                if (_playerControlSystem != null) { return _playerControlSystem; }

                _playerControlSystem = Object.FindObjectOfType<PlayerControlSystem>();
                if (_playerControlSystem == null)
                {
                    Debug.Log(new Exception($"[{nameof(GameSystems)}] - Couldn't find player control system"));
                }

                return _playerControlSystem;
            }
        }
    }
}