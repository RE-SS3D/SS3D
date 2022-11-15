using SS3D.Core.Behaviours;
using System.Collections.Generic;
using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Logging;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.GameModes.Objectives;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;

namespace SS3D.Systems.GameModes
{
    public sealed class GamemodeSystem : NetworkedSystem
    {
        public List<GamemodeObjective> PossibleObjectives;

        private List<GamemodeObjective> _createdObjectives;

        protected override void OnStart()
        {
            base.OnStart();

            Setup();
        }

        [Server]
        private void Setup()
        {
            SpawnReadyPlayersEvent.AddListener(HandleReadyPlayersChanged);
            ObjectiveStatusChangedEvent.AddListener(HandleObjectiveStatusChanged);
            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            if (e.RoundState == RoundState.Ongoing)
            {
                GenerateObjectiveList();
            }
        }

        public void FinishRound()
        {

        }

        [Server]
        private void GenerateObjectiveList()
        {
            List<GamemodeObjectiveData> objectives = new();
            foreach (GamemodeObjective gamemodeObjective in PossibleObjectives)
            {
                objectives.Add(new(gamemodeObjective));
            }

            RpcGenerateObjectiveList(objectives);
        }

        [ObserversRpc]
        private void RpcGenerateObjectiveList(List<GamemodeObjectiveData> gamemodeObjectives)
        {
            Punpun.Say(this, "ObjectiveList Generated");
        }

        private void HandleObjectiveStatusChanged(ref EventContext context, in ObjectiveStatusChangedEvent e)
        {
            Punpun.Say(this, e.Objective.Title + " - " + e.Objective.Status, Logs.ServerOnly);
        }

        private void HandleReadyPlayersChanged(ref EventContext context, in SpawnReadyPlayersEvent spawnReadyPlayersEvent)
        {
            List<string> players = spawnReadyPlayersEvent.ReadyPlayers;
        }
    }
}
