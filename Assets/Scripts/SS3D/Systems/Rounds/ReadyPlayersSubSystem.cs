using System.Collections.Generic;
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
    /// Sets what players are ready or not, and who will watch the next round as an observer
    /// (<c>observer.md</c> §2). Ready and observing exclude each other.
    /// </summary>
    public class ReadyPlayersSubSystem : NetworkSubSystem
    {
        [SyncObject] private readonly SyncList<Player> _readyPlayers = new();

        /// <summary>Players who chose Observe before the round; they enter it as ghosts.</summary>
        [SyncObject] private readonly SyncList<Player> _observingPlayers = new();

        public override void OnStartServer()
        {
            base.OnStartServer();

            ServerManager.RegisterBroadcast<ChangePlayerReadyMessage>(HandleChangePlayerReady);
            ServerManager.RegisterBroadcast<ChangePlayerObservingMessage>(HandleChangePlayerObserving);

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

            // Sandbox rule: everyone connected joins, ready or not, except those who chose to observe.
            bool allConnected = SubSystems.TryGet(out ServerSettingsService settings) && settings.Current.AllConnectedReady;
            List<Player> observers = _observingPlayers.Where(p => p != null).ToList();
            List<Player> spawning = (allConnected
                ? SubSystems.Get<PlayerSubSystem>().OnlinePlayers.ToList()
                : _readyPlayers.ToList()).Where(p => p != null && !observers.Contains(p)).ToList();
            SpawnReadyPlayersEvent spawnReadyPlayersEvent = new(spawning, observers);
            spawnReadyPlayersEvent.Invoke(this);

            _readyPlayers.Clear();
            _observingPlayers.Clear();
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

            _observingPlayers.Remove(player);
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
                    _observingPlayers.Remove(player);
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
            RoundState roundState = SubSystems.Get<RoundSubSystem>().CurrentRoundState;
            if (roundState is RoundState.Ongoing or RoundState.Ending)
            {
                return;
            }

            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(sender);
            if (player == null)
            {
                return;
            }

            SetPlayerReady(player, m.Ready);
        }

        /// <summary>Observing un-readies the player; un-observing leaves them not ready.</summary>
        [Server]
        private void SetPlayerObserving(Player player, bool observing)
        {
            bool isObserving = _observingPlayers.Contains(player);
            if (observing && !isObserving)
            {
                Log.Information(this, "player {ckey} will observe", Logs.ServerOnly, player.Ckey);
                _readyPlayers.Remove(player);
                _observingPlayers.Add(player);
            }
            else if (!observing && isObserving)
            {
                Log.Information(this, "player {ckey} will no longer observe", Logs.ServerOnly, player.Ckey);
                _observingPlayers.Remove(player);
            }
        }

        [Server]
        private void HandleChangePlayerObserving(NetworkConnection sender, ChangePlayerObservingMessage m)
        {
            RoundState roundState = SubSystems.Get<RoundSubSystem>().CurrentRoundState;
            if (roundState is RoundState.Ongoing or RoundState.Ending)
            {
                return;
            }

            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(sender);
            if (player == null)
            {
                return;
            }

            SetPlayerObserving(player, m.Observing);
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

        /// <summary>
        /// Players who would join a round starting now: everyone online when
        /// <paramref name="allConnected"/> (<see cref="ServerSettings.AllConnectedReady"/>), else the ready list.
        /// </summary>
        [Server]
        public int EffectiveReadyCount(bool allConnected) =>
            allConnected
                ? SubSystems.Get<PlayerSubSystem>().OnlinePlayers.Count(p => p != null && !_observingPlayers.Contains(p))
                : _readyPlayers.Count;

        public bool IsReady(Player player) => player != null && _readyPlayers.Contains(player);

        /// <summary>True while <paramref name="player"/> has chosen to observe the next round.</summary>
        public bool IsObserving(Player player) => player != null && _observingPlayers.Contains(player);

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

        /// <summary>Automation only: handles a <see cref="ChangePlayerObservingMessage"/> as if <paramref name="sender"/> sent it.</summary>
        [Server]
        public void ChangePlayerObservingMessageStubBroadcast(NetworkConnection sender, ChangePlayerObservingMessage m)
        {
            HandleChangePlayerObserving(sender, m);
        }
#endif
    }
}