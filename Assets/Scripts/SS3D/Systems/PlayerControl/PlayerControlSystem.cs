using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl.Messages;
using UnityEngine;

namespace SS3D.Systems.PlayerControl
{
    /// <summary>
    /// Controls the player flux, when users want to authenticate, rejoin the game, leave the game
    /// </summary>
    public sealed class PlayerControlSystem : NetworkedSystem
    {
        [SerializeField] private GameObject _soulPrefab;

        // TODO: SyncVar because client admins will probably need these, we can later restrict them from the normal user with the permissions system
        [SyncObject]
        private readonly SyncList<Soul> _serverSouls = new();

        public string GetCkey(NetworkConnection conn) => _serverSouls.Find(soul => soul.Owner == conn)?.Ckey;
        public Soul GetSoul(string ckey) => _serverSouls.Find(soul => soul.Ckey == ckey);

        protected override void OnStart()
        {
            base.OnStart();
            
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            InstanceFinder.ServerManager.RegisterBroadcast<UserAuthorizationMessage>(HandleAuthorizePlayer);
        }

        /// <summary>
        /// Used by the server to validate credentials and reassign souls to clients.
        /// TODO: Actual authentication
        /// </summary>
        /// <param name="conn">Network connection</param>
        /// <param name="userAuthorizationMessage">struct containing the ckey and the connection that sent it</param>
        [Server]
        private void HandleAuthorizePlayer(NetworkConnection conn, UserAuthorizationMessage userAuthorizationMessage)
        {
            string ckey = userAuthorizationMessage.Ckey;

            Soul match = GetSoul(ckey);

            if (match == null)
            {
                Punpun.Say(this, $"No Soul match for {ckey} found, creating a new one", Logs.ServerOnly);

                match = Instantiate(_soulPrefab).GetComponent<Soul>();
                match.SetCkey(string.Empty, ckey, true);

                _serverSouls.Add(match);

                ServerManager.Spawn(match.gameObject);
            }
            else
            {
                Punpun.Say(this, $"Soul match for {ckey} found, reassigning to client", Logs.ServerOnly);
            }

            NetworkObject networkObject = match.NetworkObject;
            networkObject.GiveOwnership(conn);

            UserJoinedServerMessage userJoinedServerMessage = new(match.Ckey);
            ServerManager.Broadcast(userJoinedServerMessage);         
        }
    }
}