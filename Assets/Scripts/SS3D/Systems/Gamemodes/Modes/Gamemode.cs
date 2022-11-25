using System;
using SS3D.Logging;
using System.Collections.Generic;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Gamemodes;
using UnityEngine;

namespace SS3D.Systems.GameModes.Modes
{
    /// <summary>
    /// Executes all gamemode related business. Manipulates the gamemode objectives
    /// </summary>
    public class Gamemode : ScriptableObject, IGamemode
    {
        public event Action OnFinished;
        public event Action OnInitialized;
        public event Action<GamemodeObjective> OnObjectiveUpdated;
        
        /// <summary>
        /// All possible objective in the round
        /// </summary>
        [SerializeField] private List<GamemodeObjective> _possibleObjectives;
        [SerializeField] private List<GamemodeObjective> _roundObjectives;
        
        /// <summary>
        /// All the antagonists spawned in the round
        /// </summary>
        public List<string> RoundAntagonists { get; set; }

        public List<GamemodeObjective> RoundObjectives => _roundObjectives;                                                                     

        /// <summary>
        /// Initializes the gamemode, it is virtual so custom initialization is possible.
        /// </summary>
        public virtual void InitializeGamemode()
        {
            RoundAntagonists = new List<string>();

            //CreateObjectives();

            OnInitialized?.Invoke();
        }

        /// <summary>
        /// Creates the objectives for the round players
        /// </summary>
        protected virtual void CreateObjectives(List<NetworkConnection> players)
        {
            EntitySpawnSystem entitySpawnSystem = SystemLocator.Get<EntitySpawnSystem>();
            List<PlayerControllable> spawnedPlayers = entitySpawnSystem.SpawnedPlayers;

            // TODO: Add objective attributing to each player
        }

        /// <summary>
        /// Creates a new objective
        /// </summary>
        /// <param name="objective"></param>
        protected virtual void CreateObjective(GamemodeObjective objective, NetworkConnection author)
        {
            GamemodeObjective objectiveInstance = Instantiate(objective);

            objectiveInstance.OnGamemodeObjectiveUpdated += HandleGamemodeObjectiveUpdated;

            objectiveInstance.SetAuthor(author);
            objectiveInstance.InitializeObjective();
        }

        protected virtual void FinalizeObjectives()
        {
            int completedObjectives = 0;
            foreach (GamemodeObjective gamemodeObjective in _roundObjectives)
            {
                ObjectiveStatus status = gamemodeObjective.Status;

                if (status != ObjectiveStatus.InProgress && status != ObjectiveStatus.Cancelled)
                {
                    completedObjectives++;
                }

                Punpun.Say(this, gamemodeObjective.Title + " - " + status);
            }

            Punpun.Say(this, "Objectives Completed: " + completedObjectives + "/" + _roundObjectives.Count);
            if (completedObjectives != _roundObjectives.Count)
            {
                return;
            }
        }

        private void HandleGamemodeObjectiveUpdated(GamemodeObjective objective)
        {
            OnObjectiveUpdated?.Invoke(objective);
        }

        /// <summary>
        /// Finishes the gamemode, it is virtual, so custom finishing is possible
        /// </summary>
        public void FinalizeGamemode()
        {
            // TEMPORARY: The gamemode will not finish the round
            OnFinished?.Invoke();
        }

        /// <summary>
        /// Fails all objectives once the round ends
        /// </summary>
        public void FailOnGoingObjectives()
        {
            foreach (GamemodeObjective gamemodeObjective in _roundObjectives)
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
