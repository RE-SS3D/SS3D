using System.Collections.Generic;
using SS3D.Core.Behaviours;
using Coimbra.Services.Events;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.GameModes.Modes;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Rounds.Messages;
using UnityEngine;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// Controls the gamemode that the round will use. Does all the networking magic.
    /// </summary>
    public sealed class GamemodeSystem : NetworkSystem
    {
        /// <summary>
        /// The gamemode that is being used.
        /// </summary>
        [SerializeField] private Gamemode _gamemode;

        /// <summary>
        /// Antagonist list in the round. Currently unused.
        /// </summary>
        public List<string> Antagonists => _gamemode.RoundAntagonists;

        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            CmdGetCurrentClientObjectives();
        }

        /// <summary>
        /// Prepares the gamemode.
        /// </summary>
        [Server]
        private void Setup()
        {   
            _eventHandles.Add(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
            _eventHandles.Add(SpawnedPlayersUpdated.AddListener(HandleSpawnedPlayersChanged));
            _eventHandles.Add(InitialPlayersSpawnedEvent.AddListener(HandleInitialPlayersSpawned));
        }

        /// <summary>
        /// Initializes the gamemode.
        /// </summary>
        [Server]
        private void InitializeGamemode()
        {
            // Creates an instance of the SO, to avoid using the file.
            _gamemode = Instantiate(_gamemode);

            _gamemode.OnInitialized += HandleGamemodeInitialized;
            _gamemode.OnFinished += HandleGamemodeFinalized;
            _gamemode.OnObjectiveUpdated += HandleObjectiveUpdated;

            _gamemode.InitializeGamemode();
        }

        /// <summary>
        /// Finalizes the gamemode.
        /// </summary>
        [Server]
        private void FinalizeGamemode()
        {
            if (_gamemode == null)
            {
                return;
            }

            _gamemode.OnInitialized -= HandleGamemodeInitialized;
            _gamemode.OnFinished -= HandleGamemodeFinalized;
            _gamemode.OnObjectiveUpdated -= HandleObjectiveUpdated;
            
            _gamemode.FinalizeGamemode();

            // Removes the current gamemode.
            _gamemode = null;
        }

        /// <summary>
        /// Finishes the round.
        /// </summary>
        [Server]
        public void EndRound()
        {
            _gamemode.FailOnGoingObjectives();

            ChangeRoundStateMessage message = new(false);
            ClientManager.Broadcast(message);
        }

        /// <summary>
        /// Sends an objective to all interested clients. Usually the owner and admins.
        /// </summary>
        /// <param name="objective">The objective to be sent via network message.</param>
        [Server]
        private void SendObjectiveToClients(GamemodeObjective objective)
        {
            NetworkConnection author = objective.Assignee;
            GamemodeObjectiveUpdatedMessage message = new(objective);

            // TODO Add admins as receivers of this message
            HashSet<NetworkConnection> receivers = new()
            {
                author,
                // TODO: Get admins or something 🥸
            };

            ServerManager.Broadcast(receivers, message);
        }

        /// <summary>
        /// Asks the server for the current GamemodeObjectives for a client that joined late.
        /// </summary>
        /// <param name="sender">Who asked the objectives.</param>
        [ServerRpc(RequireOwnership = false)]
        private void CmdGetCurrentClientObjectives(NetworkConnection sender = null)
        {
            List<GamemodeObjective> gamemodeObjectives = _gamemode.GetPlayerObjectives(sender);

            foreach (GamemodeObjective gamemodeObjective in gamemodeObjectives)
            {
                SendObjectiveToClients(gamemodeObjective);
            }
        }

        /// <summary>
        /// Called whenever the ready players are spawned at the start of the round.          
        /// </summary>
        [Server]
        private void HandleInitialPlayersSpawned(ref EventContext context, in InitialPlayersSpawnedEvent e)
        {
            InitializeGamemode();   
        }

        /// <summary>
        /// Called whenever a new player is spawned.
        /// </summary>
        [Server]
        private void HandleSpawnedPlayersChanged(ref EventContext context, in SpawnedPlayersUpdated e)
        {
            EntitySpawnSystem entitySpawnSystem = SystemLocator.Get<EntitySpawnSystem>();
            PlayerControllable player = entitySpawnSystem.LastSpawned;

            if (player != null)
            {
                _gamemode.CreateLateJoinObjective(player);
            }
        }

        /// <summary>
        /// Called whenever the round state is updated.
        /// </summary>
        [Server]
        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            switch (e.RoundState)
            {
                case RoundState.Ending:
                    FinalizeGamemode();
                    break;
            }
        }

        /// <summary>
        /// Called once the gamemode was finished.
        /// </summary>
        /// <param name="roundObjectives">The objectives on the finished round.</param>
        [Server]
        private void HandleGamemodeFinalized(List<GamemodeObjective> roundObjectives)
        {
             // TODO: Send clients all objectives in that round.
        }

        /// <summary>
        /// Called once the gamemode is initialized.
        /// </summary>
        [Server]
        private void HandleGamemodeInitialized()
        {
            // Not sure yet. Probably no logic will run here.
        }

        /// <summary>
        /// Called when an objective is updated, sends it to the client.
        /// </summary>
        /// <param name="objective">The objective to be sent.</param>
        [Server]
        private void HandleObjectiveUpdated(GamemodeObjective objective)
        {
            SendObjectiveToClients(objective);
        }
    }
}