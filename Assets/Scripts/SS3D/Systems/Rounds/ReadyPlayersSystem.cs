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
    public class ReadyPlayersSystem : NetworkSystem
    {
        [SyncObject] private readonly SyncList<Soul> _readyPlayers = new();

        public override void OnStartServer()
        {
            base.OnStartServer();

            ServerManager.RegisterBroadcast<ChangePlayerReadyMessage>(HandleChangePlayerReady);

            AddHandle(OnlineSoulsChanged.AddListener(HandleUserLeftServer));
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
        private void RemoveReadyPlayer(Soul soul, ChangeType changeType)
        {
            if (changeType == ChangeType.Addition)
            {
                return;
            }

            if (soul == null)
            {
                return;
            }

            if (_readyPlayers.SingleOrDefault(match => match == soul) != null)
            {
                _readyPlayers.Remove(soul);
            }
        }

        /// <summary>
        /// Sets the player ready state
        /// </summary>
        /// <param name="soul">The player's Soul</param>
        /// <param name="ready">Is the player ready</param>
        [Server]
        private void SetPlayerReady(Soul soul, bool ready)
        {
            bool soulIsReady = _readyPlayers.Contains(soul);

            switch (ready)
            {
                case true when !soulIsReady:
                    Punpun.Say(this, $"player is {soul.Ckey} is ready", Logs.ServerOnly);
                    _readyPlayers.Add(soul);
                    break;
                case false when soulIsReady:
                    Punpun.Say(this, $"player is {soul.Ckey} is not ready", Logs.ServerOnly);
                    _readyPlayers.Remove(soul);
                    break;
            }
        }

        [Server]
        private void HandleChangePlayerReady(NetworkConnection sender, ChangePlayerReadyMessage m)
        {
            Soul soul = SystemLocator.Get<PlayerSystem>().GetSoul(m.Ckey);

            SetPlayerReady(soul, m.Ready);
        }

        private void HandleReadyPlayersChanged(SyncListOperation op, int index, Soul oldItem, Soul newItem, bool asServer)
        {
            SyncReadyPlayers();
        }

        [Server]
        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            InvokeSpawnReadyPlayers(e.RoundState);
        }

        [Server]
        private void HandleUserLeftServer(ref EventContext context, in OnlineSoulsChanged e)
        {
            RemoveReadyPlayer(e.ChangedSoul, e.ChangeType);
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