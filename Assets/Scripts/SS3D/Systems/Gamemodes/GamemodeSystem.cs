using System.Collections.Generic;
using SS3D.Core.Behaviours;
using Coimbra.Services.Events;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.GameModes.Modes;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.GameModes.Objectives;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Rounds.Messages;
using UnityEngine;

namespace SS3D.Systems.GameModes
{
    /// <summary>
    /// Controls the gamemode that the round will use
    /// </summary>
    public sealed class GamemodeSystem : NetworkedSystem
    {
        /// <summary>
        /// The gamemode that is being used
        /// </summary>
        [SerializeField] private Gamemode _gamemode;

        public List<string> Antagonists => _gamemode.Antagonists;

        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        [Server]
        private void Setup()
        {
            RoundStateUpdated.AddListener(HandleRoundStateUpdated);

            _gamemode = Instantiate(_gamemode);
        }

        [Server]
        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            switch (e.RoundState)
            {
                case RoundState.Ongoing:
                    InitializeGamemode();
                    break;
                case RoundState.Ending:
                    FinalizeGamemode();
                    break;
            }
        }

        /// <summary>
        /// Finalizes the Gamemode
        /// </summary>
        [Server]
        private void FinalizeGamemode()
        {
            _gamemode.OnInitialized -= HandleGamemodeInitialized;
            _gamemode.OnFinished -= HandleGamemodeFinalized;
            _gamemode.OnObjectiveInitialized -= HandleObjectiveInitialized;
            
            _gamemode.FinalizeGamemode();
        }

        /// <summary>
        /// Initializes the gamemode
        /// </summary>
        [Server]
        private void InitializeGamemode()
        {
            _gamemode.OnInitialized += HandleGamemodeInitialized;
            _gamemode.OnFinished += HandleGamemodeFinalized;
            _gamemode.OnObjectiveInitialized += HandleObjectiveInitialized;

            _gamemode.InitializeGamemode();
        }

        /// <summary>
        /// Finishes the round
        /// </summary>
        [Server]
        public void EndRound()
        {
            _gamemode.FailOnGoingObjectives();

            ChangeRoundStateMessage changeRoundStateMessage = new(false);
            ClientManager.Broadcast(changeRoundStateMessage);
        }

        [Server]
        private void HandleGamemodeFinalized()
        {
            
        }

        [Server]
        private void HandleGamemodeInitialized()
        {

        }

        [Server]
        private void HandleObjectiveInitialized(GamemodeObjective objective)
        {
            GamemodeObjectiveUpdatedMessage message = new(objective);
            NetworkConnection author = message.Objective.Author;

            // TODO Add admins as receivers of this message
            HashSet<NetworkConnection> receivers = new()
            {
                author,
                // TODO: Get admins or something
            };

            ServerManager.Broadcast(receivers, message);
        }
    }
}