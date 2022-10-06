using System.Collections.Generic;
using System.Linq;
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
        [SyncVar(OnChange = "SetReadyPlayers")] private List<Soul> _readyPlayers = new();

        public override void OnStartServer()
        {
            base.OnStartServer();

            ServerManager.RegisterBroadcast<ChangePlayerReadyMessage>(HandleChangePlayerReady);
            ServerManager.RegisterBroadcast<UserLeftServerMessage>(HandleUserLeftServer);
        }

        private void HandleUserLeftServer(NetworkConnection sender, UserLeftServerMessage m)
        {
            Soul soul = GameSystems.Get<PlayerControlSystem>().GetSoulByCkey(m.Ckey);

            if (_readyPlayers.SingleOrDefault(match => match == soul))
            {
                _readyPlayers.Remove(soul);
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
            if (ready)
            {
                if (!_readyPlayers.SingleOrDefault(match => match == soul))
                {
                    Punpun.Say(this, $"player is {soul.Ckey} is ready", LogType.ServerOnly);
                    _readyPlayers.Add(soul);
                }
            }

            else
            {
                if (_readyPlayers.SingleOrDefault(match => match == soul))
                {
                    Punpun.Say(this, $"player is {soul.Ckey} is not ready", LogType.ServerOnly);
                    _readyPlayers.Remove(soul);
                }
            }
        }

        private void SetReadyPlayers(List<Soul> oldReadyPlayers, List<Soul> newReadyPlayers, bool asServer)
        {
            _readyPlayers = newReadyPlayers;

            ReadyPlayersChanged readyPlayersChanged = new ReadyPlayersChanged(_readyPlayers);
            readyPlayersChanged.Invoke(this);
        }
    }
}