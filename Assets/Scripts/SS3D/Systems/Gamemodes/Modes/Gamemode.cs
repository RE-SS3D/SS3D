using FishNet.Object;
using SS3D.Logging;
using SS3D.Systems.GameModes.Objectives;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.GameModes.Modes
{
    public class Gamemode : ScriptableObject
    {
        public List<GamemodeObjective> PossibleObjectives;
        [HideInInspector] public GamemodeSystem GamemodeSystem;

        [Server]
        public void CheckObjectivesCompleted()
        {
            int completedObjectives = 0;
            foreach (GamemodeObjective gamemodeObjective in PossibleObjectives)
            {
                if (gamemodeObjective.Status != ObjectiveStatus.InProgress &&
                    gamemodeObjective.Status != ObjectiveStatus.Cancelled)
                {
                    completedObjectives++;
                }
                Punpun.Say(this, gamemodeObjective.Title + " - " + gamemodeObjective.Status);
            }

            Punpun.Say(this, "Objectives Completed: " + completedObjectives + "/" + PossibleObjectives.Count);
            if (completedObjectives == PossibleObjectives.Count)
            {
                GamemodeSystem.FinishRound();
            }
        }
    }
}
