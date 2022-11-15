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
            if (e.RoundState == RoundState.Ending)
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
            GamemodeObjectiveData objectives = new(PossibleObjectives[0]);

            RpcGenerateObjectiveList(objectives);
        }

        [ObserversRpc]
        private void RpcGenerateObjectiveList(GamemodeObjectiveData gamemodeObjectives)
        {
             Punpun.Panic(this, "recebi");
        }

        private void HandleObjectiveStatusChanged(ref EventContext context, in ObjectiveStatusChangedEvent e)
        {
            Punpun.Say(this, e.Objective.Title, Logs.ServerOnly);
        }

        private void HandleReadyPlayersChanged(ref EventContext context, in SpawnReadyPlayersEvent spawnReadyPlayersEvent)
        {
            List<string> players = spawnReadyPlayersEvent.ReadyPlayers;
        }
    }
}
