using System.Linq;
using Coimbra.Services.Events;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.PlayerControl.Messages;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Rounds.Messages;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// Sets what players are ready or not.
    /// </summary>
    public class RoundPlayerSystem : NetworkedSystem
    {
        [SyncObject] private readonly SyncList<string> _readyPlayers = new();

        public override void OnStartServer()
        {
            base.OnStartServer();

            ServerManager.RegisterBroadcast<ChangePlayerReadyMessage>(HandleChangePlayerReady);
            ServerManager.RegisterBroadcast<UserLeftServerMessage>(HandleUserLeftServer);

            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
            _readyPlayers.OnChange += SyncReadyPlayers;
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            RoundState roundState = e.RoundState;

            if (roundState == RoundState.Ongoing)
            {
                // Spawn players idk
                _readyPlayers.Clear();
            }
        }

        private void HandleUserLeftServer(NetworkConnection sender, UserLeftServerMessage m)
        {
            Soul soul = GameSystems.Get<PlayerControlSystem>().GetSoulByCkey(m.Ckey);

            if (_readyPlayers.SingleOrDefault(match => match == soul.Ckey) != null)
            {
                _readyPlayers.Remove(soul.Ckey);
            }
        }

        private void HandleChangePlayerReady(NetworkConnection sender, ChangePlayerReadyMessage m)
        {
            Soul soul = GameSystems.Get<PlayerControlSystem>().GetSoulByCkey(m.Ckey);

            SetPlayerReady(soul, m.Ready);
        }

        /// <summary>
        /// Sets the player ready state
        /// </summary>
        /// <param name="soul">The player's Soul</param>
        /// <param name="ready">Is the player ready</param>
        [Server]
        private void SetPlayerReady(Soul soul, bool ready)
        {
            bool soulIsReady = _readyPlayers.Contains(soul.Ckey);

            switch (ready)
            {
                case true when !soulIsReady:
                    Punpun.Say(this, $"player is {soul.Ckey} is ready", LogType.ServerOnly);
                    _readyPlayers.Add(soul.Ckey);
                    break;
                case false when soulIsReady:
                    Punpun.Say(this, $"player is {soul.Ckey} is not ready", LogType.ServerOnly);
                    _readyPlayers.Remove(soul.Ckey);
                    break;
            }
        }

        [Server]
        private void SyncReadyPlayers(SyncListOperation op, int index, string s, string newItem1, bool asServer)
        {
            ReadyPlayersChanged readyPlayersChanged = new(_readyPlayers.ToList());

            ServerManager.Broadcast(readyPlayersChanged);
        }
    }
}