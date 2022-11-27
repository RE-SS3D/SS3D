using System;
using SS3D.Logging;
using System.Collections.Generic;
using System.Linq;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Gamemodes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SS3D.Systems.GameModes.Modes
{
    /// <summary>
    /// Executes all gamemode related business. Manipulates the gamemode objectives.
    /// All that is run here is server-only.
    /// </summary>
    public class Gamemode : ScriptableObject, IGamemode
    {
        /// <summary>
        /// Called when we finalize the gamemode, sends all the objectives in that round.
        /// </summary>
        public event Action<List<GamemodeObjective>> OnFinished;
        /// <summary>
        /// Called when we initialize the gamemode.
        /// </summary>
        public event Action OnInitialized;
        /// <summary>
        /// Called whenever a gamemode objective is updated, used for quick access by the GamemodeSystem.
        /// </summary>
        public event Action<GamemodeObjective> OnObjectiveUpdated;
        
        /// <summary>
        /// All possible objective in the round.
        /// </summary>
        [SerializeField] private GamemodeObjectiveCollection _possibleObjectives;

        /// <summary>
        /// The current attributed objectives in a round.
        /// </summary>
        [SerializeField] private List<GamemodeObjective> _roundObjectives;
        
        /// <summary>
        /// All the antagonists spawned in the round.
        /// </summary>
        public List<string> RoundAntagonists { get; set; }

        /// <summary>
        /// The objectives in the current round.
        /// </summary>
        public List<GamemodeObjective> RoundObjectives => _roundObjectives;                                                                     

        /// <summary>
        /// Initializes the gamemode, it is virtual so custom initialization is possible.
        /// </summary>
        public virtual void InitializeGamemode()
        {
            RoundAntagonists = new List<string>();
            
            CreateObjectives();

            OnInitialized?.Invoke();
        }

        /// <summary>
        /// Finishes the gamemode, it is virtual, so custom finishing is possible
        /// </summary>
        public virtual void FinalizeGamemode()
        {
            FinalizeObjectives(); 

            OnFinished?.Invoke(_roundObjectives);
        }

        /// <summary>
        /// Finalizes all objectives in the round.
        /// </summary>
        protected virtual void FinalizeObjectives()
        {
            int completedObjectives = 0;

            foreach (GamemodeObjective objective in _roundObjectives)
            {
                ObjectiveStatus status = objective.Status;
                bool completed = objective.Status == ObjectiveStatus.Success;

                if (completed)
                {
                    completedObjectives++;
                }

                Punpun.Say(this, objective.Title + " - " + status);
            }

            Punpun.Say(this, "Objectives Completed: " + completedObjectives + "/" + _roundObjectives.Count);
        }

        /// <summary>
        /// Creates the objectives for the round players. This base method assigns a single objective for each player.
        /// </summary>
        protected virtual void CreateObjectives()
        {
            EntitySpawnSystem entitySpawnSystem = SystemLocator.Get<EntitySpawnSystem>();
            List<PlayerControllable> spawnedPlayers = entitySpawnSystem.SpawnedPlayers;

            _possibleObjectives = _possibleObjectives.Clone();
            List<GamemodeObjectiveCollectionEntry> objectivesEntries = _possibleObjectives.Entries;

            while (spawnedPlayers.Count != 0 && objectivesEntries.Count != 0)
            {
                int randomObjectiveIndex = Random.Range(0, objectivesEntries.Count);

                GamemodeObjective objective = objectivesEntries[randomObjectiveIndex].GamemodeObjective;
                objectivesEntries.RemoveAt(randomObjectiveIndex);

                PlayerControllable player = spawnedPlayers.First();
                spawnedPlayers.RemoveAt(0);

                objective.SetAuthor(player.ControllingSoul.Owner);
                objective.OnGamemodeObjectiveUpdated += HandleGamemodeObjectiveUpdated;

                objective.InitializeObjective();
            }
        }

        /// <summary>
        /// Fails all objectives once the round ends.
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

        /// <summary>
        /// Called whenever an objective is updated to tell the GamemodeSystem an objective was updated.
        /// </summary>
        /// <param name="objective">The objective that was updated.</param>
        private void HandleGamemodeObjectiveUpdated(GamemodeObjective objective)
        {
            OnObjectiveUpdated?.Invoke(objective);
        }
    }
}
