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
using SS3D.Systems.PlayerControl.Events;
using SS3D.Systems.PlayerControl.Messages;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Rounds.Messages;
using RoundStateUpdated = SS3D.Systems.Rounds.Events.RoundStateUpdated;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// Sets what players are ready or not.
    /// </summary>
    public class ReadyPlayersSystem : NetworkedSystem
    {
        [SyncObject] private readonly SyncList<string> _readyPlayers = new();

        public override void OnStartServer()
        {
            base.OnStartServer();

            ServerManager.RegisterBroadcast<ChangePlayerReadyMessage>(HandleChangePlayerReady);

            OnlineSoulsChanged.AddListener(HandleUserLeftServer);
            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            _readyPlayers.OnChange += SetReadyPlayers;
            SyncReadyPlayers();
        }

        [Server]
        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            RoundState roundState = e.RoundState;

            if (roundState != RoundState.Ongoing)
            {
                return;
            }

            SpawnReadyPlayersEvent spawnReadyPlayersEvent = new(_readyPlayers.ToList());
            spawnReadyPlayersEvent.Invoke(this);

            _readyPlayers.Clear();
        }

        [Server]
        private void HandleUserLeftServer(ref EventContext context, in OnlineSoulsChanged e)
        {
            if (e.ChangeType == ChangeType.Addition)
            {
                return;
            }

            Soul soul = e.Changed;

            if (soul == null)
            {
                return;
            }

            if (_readyPlayers.SingleOrDefault(match => match == soul.Ckey) != null)
            {
                _readyPlayers.Remove(soul.Ckey);
            }
        }

        [Server]
        private void HandleChangePlayerReady(NetworkConnection sender, ChangePlayerReadyMessage m)
        {
            Soul soul = SystemLocator.Get<PlayerControlSystem>().GetSoul(m.Ckey);

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
                    Punpun.Say(this, $"player is {soul.Ckey} is ready", Logs.ServerOnly);
                    _readyPlayers.Add(soul.Ckey);
                    break;
                case false when soulIsReady:
                    Punpun.Say(this, $"player is {soul.Ckey} is not ready", Logs.ServerOnly);
                    _readyPlayers.Remove(soul.Ckey);
                    break;
            }
        }

        private void SetReadyPlayers(SyncListOperation op, int index, string s, string newItem1, bool asServer)
        {
            SyncReadyPlayers();
        }

        private void SyncReadyPlayers()
        {
            ReadyPlayersChanged readyPlayersChanged = new(_readyPlayers.ToList());
            readyPlayersChanged.Invoke(this);
        }
    }
}