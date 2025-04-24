using System.Collections.Generic;
using SS3D.Core.Behaviours;
using Coimbra.Services.Events;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.GameModes.Modes;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Rounds.Messages;
using UnityEngine;
using SS3D.Systems.PlayerControl;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// Controls the gamemode that the round will use. Does all the networking magic.
    /// </summary>
    public sealed class GamemodeSubSystem : NetworkSubSystem
    {
        /// <summary>
        /// The gamemode that is being used.
        /// </summary>
        [SyncVar] 
        [SerializeField]
        private Gamemode _gamemode;

        /// <summary>
        /// Antagonist list in the round. Currently unused.
        /// </summary>
        public List<string> Antagonists => _gamemode.RoundAntagonists;

        protected override void OnStart()
        {
            base.OnStart();
            if (base.IsServer) Setup();
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
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
            AddHandle(SpawnedPlayersUpdated.AddListener(HandleSpawnedPlayersChanged));
            AddHandle(InitialPlayersSpawned.AddListener(HandleInitialPlayersSpawned));
        }

        /// <summary>
        /// Initializes the gamemode.
        /// </summary>
        [Server]
        private void InitializeGamemode()
        {
            // Creates an instance of the SO, to avoid using the file. 
            _gamemode = Instantiate(_gamemode);

            // Subscribe to Gamemode events
            _gamemode.OnInitialized += HandleGamemodeInitialized;
            _gamemode.OnFinished += HandleGamemodeFinalized;

            // Get systems we need to load player data
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();

            // Get list of players ready to spawn (by Ckey).
            List<Entity> playersToAssign = entitySystem.SpawnedPlayers;
            List<string> playerCkeys = new List<string>();
            for (int i = 0; i < playersToAssign.Count; i++)
            {
                playerCkeys.Add(playerSystem.GetCkey(playersToAssign[i].Owner));
            }

            // Actually initialize the gamemode
            _gamemode.InitializeGamemode(playerCkeys);

            // Add event listeners
            foreach (var objective in _gamemode.RoundObjectives)
            {
                objective.AddEventListeners();
            }

            // Subscribe to Objective events - we cannot do this before initializing the gamemode.
            _gamemode.OnObjectiveUpdated += HandleObjectiveUpdated;

            // Update all clients with the objectives
            foreach (GamemodeObjective gamemodeObjective in _gamemode.RoundObjectives)
            {
                SendObjectiveToClients(gamemodeObjective);
            }

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
            _gamemode.ResetGamemode();

            // Removes the current gamemode.
            // _gamemode = null;
        }

        /// <summary>
        /// Finishes the round.
        /// </summary>
        [Server]
        public void EndRound()
        {
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
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();

            NetworkConnection author = playerSystem.GetPlayer(objective.AssigneeCkey).Owner;
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
            if (_gamemode == null)
            {
                return;
            }

            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();

            List<GamemodeObjective> gamemodeObjectives = _gamemode.GetPlayerObjectives(playerSystem.GetCkey(sender));

            if (gamemodeObjectives == null)
            {
                return;
            }

            foreach (GamemodeObjective gamemodeObjective in gamemodeObjectives)
            {
                SendObjectiveToClients(gamemodeObjective);
            }
        }

        /// <summary>
        /// Called whenever the ready players are spawned at the start of the round.          
        /// </summary>
        [Server]
        private void HandleInitialPlayersSpawned(ref EventContext context, in InitialPlayersSpawned e)
        {
            InitializeGamemode();   
        }

        /// <summary>
        /// Called whenever a new player is spawned.
        /// </summary>
        [Server]
        private void HandleSpawnedPlayersChanged(ref EventContext context, in SpawnedPlayersUpdated e)
        {
            // Exit early if the gamemode has not yet been initialized.
            if (!_gamemode.IsInitialized)
            {
                return;
            }

            // Retrieve the Ckey of the newly spawned player.
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            string newPlayerCkey = SubSystems.Get<PlayerSubSystem>()?.GetCkey(entitySystem.LastSpawned.Owner);

            // Assign late join objectives to the new player
            if (newPlayerCkey != null)
            {
                _gamemode.CreateLateJoinObjective(newPlayerCkey);
                _gamemode.AddEventListenersForLateJoinObjectives(newPlayerCkey);
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