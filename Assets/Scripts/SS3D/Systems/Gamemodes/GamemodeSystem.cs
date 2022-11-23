using SS3D.Core.Behaviours;
using System.Collections.Generic;
using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Logging;
using SS3D.Systems.GameModes.Modes;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.GameModes.Objectives;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Rounds.Messages;
using UnityEngine;
using SS3D.Systems.GameModes.UI;

namespace SS3D.Systems.GameModes
{
    public sealed class GamemodeSystem : NetworkedSystem
    {
        public Gamemode Gamemode;
        public GamemodeUI GamemodeUI;

        protected override void OnStart()
        {
            base.OnStart();

            Setup();
        }

        [Server]
        private void Setup()
        {
            Gamemode.GamemodeSystem = this;
            Gamemode.InitializeGamemode();

            GamemodeUI = Instantiate(GamemodeUI);
            GamemodeUI.transform.parent = this.transform;

            ObjectiveStatusChangedEvent.AddListener(HandleObjectiveStatusChanged);
            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
        }

        [Server]
        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            if (e.RoundState == RoundState.Ongoing)
            {
                GenerateObjectives();
                GamemodeUI.SetMainText("You are the Traitor!", Color.red);
                GamemodeUI.FadeOutMainText();
            }
        }

        [Server]
        public void FinishRound()
        {
            Gamemode.FailOnGoingObjectives();

            ChangeRoundStateMessage changeRoundStateMessage = new(false);
            ClientManager.Broadcast(changeRoundStateMessage);
        }

        [Server]
        private void GenerateObjectives()
        {
            foreach (GamemodeObjective gamemodeObjective in Gamemode.PossibleObjectives)
            {
                gamemodeObjective.InitializeObjective();
                GamemodeUI.ObjectivesUI.AddObjective(gamemodeObjective.Title, gamemodeObjective);
            }
        }

        [Server]
        private void HandleObjectiveStatusChanged(ref EventContext context, in ObjectiveStatusChangedEvent e)
        {
            Gamemode.CheckObjectivesCompleted();
        }
    }
}
