using System;
using SS3D.Logging;
using SS3D.Systems.GameModes.Objectives;
using System.Collections.Generic;
using SS3D.Core;
using UnityEngine;

namespace SS3D.Systems.GameModes.Modes
{
    /// <summary>
    /// Executes all gamemode related business.
    /// </summary>
    public class Gamemode : ScriptableObject, IGamemode
    {
        public event Action OnFinished;
        public event Action OnInitialized;

        public event Action<GamemodeObjective> OnObjectiveInitialized; 
        /// <summary>
        /// All possible objective in the round
        /// </summary>
        [SerializeField] private List<GamemodeObjective> _possibleObjectives;
        [SerializeField] private List<GamemodeObjective> _activeObjectives;
        public List<string> Antagonists { get; set; }

        public List<GamemodeObjective> ActiveObjectives => _activeObjectives;                                                                     

        /// <summary>
        /// Initializes the gamemode, it is virtual so custom initialization is possible.
        /// </summary>
        public virtual void InitializeGamemode()
        {
            Antagonists = new List<string>();

            foreach (GamemodeObjective objective in _activeObjectives)
            {
                objective.InitializeObjective();
                OnObjectiveInitialized?.Invoke(objective);
            }

            OnInitialized?.Invoke();
        }

        /// <summary>
        /// Finishes the gamemode, it is virtual, so custom finishing is possible
        /// </summary>
        public void FinalizeGamemode()
        {
            int completedObjectives = 0;
            foreach (GamemodeObjective gamemodeObjective in _activeObjectives)
            {
                ObjectiveStatus status = gamemodeObjective.Status;

                if (status != ObjectiveStatus.InProgress && status != ObjectiveStatus.Cancelled)
                {
                    completedObjectives++;
                }

                Punpun.Say(this, gamemodeObjective.Title + " - " + status);
            }

            Punpun.Say(this, "Objectives Completed: " + completedObjectives + "/" + _activeObjectives.Count);
            if (completedObjectives != _activeObjectives.Count)
            {
                return;
            }

            // TEMPORARY: The gamemode will not finish the round
            OnFinished?.Invoke();
        }

        /// <summary>
        /// Fails all objectives once the round ends
        /// </summary>
        public void FailOnGoingObjectives()
        {
            foreach (GamemodeObjective gamemodeObjective in _activeObjectives)
            {
                ObjectiveStatus status = gamemodeObjective.Status;

                if (status == ObjectiveStatus.InProgress)
                {
                    gamemodeObjective.Fail();
                }
            }
        }
    }
}
