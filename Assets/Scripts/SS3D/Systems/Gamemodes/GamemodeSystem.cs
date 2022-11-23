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
        [SerializeField] private GamemodeUI GamemodeUI;

        protected override void OnStart()
        {
            base.OnStart();

            Setup();
            SetupUI();
            RpcSetupUI();
        }

        [Server]
        private void Setup()
        {
            Gamemode.GamemodeSystem = this;
            Gamemode.InitializeGamemode();

            ObjectiveStatusChangedEvent.AddListener(HandleObjectiveStatusChanged);
            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
        }

        [Server]
        private void SetupUI()
        {
            GamemodeUI = Instantiate(GamemodeUI);
            GamemodeUI.transform.parent = this.transform;
        }

        [ObserversRpc]
        private void RpcSetupUI()
        {
            GamemodeUI = Instantiate(GamemodeUI);
            GamemodeUI.transform.parent = this.transform;
        }

        [Server]
        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            if (e.RoundState == RoundState.Ongoing)
            {
                GenerateObjectives();
                SetMainText("You are the Traitor!", Color.red, 3f);
            }
        }

        [Server]
        public void FinishRound()
        {
            Gamemode.FailOnGoingObjectives();

            SetMainText("The Traitors have Won!", Color.red, 3f);
            RpcSetMainText("The Traitors have Won!", Color.red, 3f);

            ChangeRoundStateMessage changeRoundStateMessage = new(false);
            ClientManager.Broadcast(changeRoundStateMessage);
        }

        [Server]
        private void GenerateObjectives()
        {
            foreach (GamemodeObjective gamemodeObjective in Gamemode.PossibleObjectives)
            {
                gamemodeObjective.InitializeObjective();
                GamemodeUI.ObjectivesView.AddObjective(gamemodeObjective.Title, gamemodeObjective);
            }
        }

        [Server]
        private void HandleObjectiveStatusChanged(ref EventContext context, in ObjectiveStatusChangedEvent e)
        {
            Gamemode.CheckObjectivesCompleted();
            UpdateObjectiveUI(e.Objective);
        }

        [Server]
        void SetMainText(string text, Color color, float timer)
        {
            GamemodeUI.SetMainText(text, color);
            GamemodeUI.FadeOutMainText(timer);
        }

        [ObserversRpc]
        void RpcSetMainText(string text, Color color, float timer)
        {
            GamemodeUI.SetMainText(text, color);
            GamemodeUI.FadeOutMainText(timer);
        }

        [Server]
        void UpdateObjectiveUI(GamemodeObjective objective)
        {
            GamemodeUI.ObjectivesView.UpdateObjective(objective);
            RpcUpdateObjectiveUI(objective);
        }

        [ObserversRpc]
        void RpcUpdateObjectiveUI(GamemodeObjective objective)
        {
            GamemodeUI.ObjectivesView.UpdateObjective(objective);
        }
    }
}
