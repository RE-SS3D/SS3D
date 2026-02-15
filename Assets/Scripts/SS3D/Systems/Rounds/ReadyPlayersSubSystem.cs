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
    public class ReadyPlayersSubSystem : NetworkSubSystem
    {
        [SyncObject] private readonly SyncList<Player> _readyPlayers = new();

        public override void OnStartServer()
        {
            base.OnStartServer();

            ServerManager.RegisterBroadcast<ChangePlayerReadyMessage>(HandleChangePlayerReady);

            AddHandle(OnlinePlayersChanged.AddListener(HandleUserLeftServer));
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            _readyPlayers.OnChange += HandleReadyPlayersChanged;
            SyncReadyPlayers();
        }

        private void InvokeSpawnReadyPlayers(RoundState roundState)
        {
            if (roundState != RoundState.Ongoing)
            {
                return;
            }

            SpawnReadyPlayersEvent spawnReadyPlayersEvent = new(_readyPlayers.ToList());
            spawnReadyPlayersEvent.Invoke(this);

            _readyPlayers.Clear();
        }

        [Server]
        private void RemoveReadyPlayer(Player player, ChangeType changeType)
        {
            if (changeType == ChangeType.Addition)
            {
                return;
            }

            if (player == null)
            {
                return;
            }

            if (_readyPlayers.SingleOrDefault(match => match == player) != null)
            {
                _readyPlayers.Remove(player);
            }
        }

        /// <summary>
        /// Sets the player ready state
        /// </summary>
        /// <param name="player">The player himself</param>
        /// <param name="ready">Is the player ready</param>
        [Server]
        private void SetPlayerReady(Player player, bool ready)
        {
            bool playerIsReady = _readyPlayers.Contains(player);

            switch (ready)
            {
                case true when !playerIsReady:
                    Log.Information(this, "player is {ckey} is ready", Logs.ServerOnly, player.Ckey);
                    _readyPlayers.Add(player);
                    break;
                case false when playerIsReady:
                    Log.Information(this, "player is {cCkey} is not ready", Logs.ServerOnly, player.Ckey);
                    _readyPlayers.Remove(player);
                    break;
            }
        }

        [Server]
        private void HandleChangePlayerReady(NetworkConnection sender, ChangePlayerReadyMessage m)
        {
            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(m.Ckey);

            SetPlayerReady(player, m.Ready);
        }

        private void HandleReadyPlayersChanged(SyncListOperation op, int index, Player oldItem, Player newItem, bool asServer)
        {
            SyncReadyPlayers();
        }

        [Server]
        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            InvokeSpawnReadyPlayers(e.RoundState);
        }

        [Server]
        private void HandleUserLeftServer(ref EventContext context, in OnlinePlayersChanged e)
        {
            RemoveReadyPlayer(e.ChangedPlayer, e.ChangeType);
        }

        /// <summary>
        /// Called by manually when the ready players are changed.
        /// </summary>
        private void SyncReadyPlayers()
        {
            ReadyPlayersChanged readyPlayersChanged = new(_readyPlayers.ToList());
            readyPlayersChanged.Invoke(this);
        }

        public int Count => _readyPlayers.Count;

#if UNITY_EDITOR
        /// <summary>
        /// This method facilitates automated testing, and is not to be used in production.
        /// It simulates a ChangePlayerReadyMessage broadcast received from a client, and
        /// is handled normally by the server. Method required because the server cannot
        /// broadcast to itself.
        /// </summary>
        /// <param name="sender">The client the message is apparently from</param>
        /// <param name="m">The ChangePlayerReadyMessage apparently broadcast</param>
        [Server]
        public void ChangePlayerReadyMessageStubBroadcast(NetworkConnection sender, ChangePlayerReadyMessage m)
        {
            HandleChangePlayerReady(sender, m);
        }
#endif
    }
}