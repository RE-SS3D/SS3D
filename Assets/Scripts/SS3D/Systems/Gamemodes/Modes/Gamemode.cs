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
        public GamemodeObjectiveCollection PossibleObjectives;

        /// <summary>
        /// The current attributed objectives in a round.
        /// </summary>
        private List<GamemodeObjective> _roundObjectives;
        
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
        /// Finalizes all objectives in the round. Checks their CheckCompletion
        /// because sometimes an item has to be in the hand of some player for an objective to be completed.
        /// </summary>
        protected virtual void FinalizeObjectives()
        {
            foreach (GamemodeObjective objective in _roundObjectives)
            {
                objective.CheckCompletion();

                ObjectiveStatus status = objective.Status;

                Punpun.Say(this, $"{objective.Title} - {status}");
            }

            int succeededObjectives = _roundObjectives.Count(objective => objective.Succeeded);
            Punpun.Say(this, $"Objectives Completed: {succeededObjectives}/{_roundObjectives.Count}");
        }

        /// <summary>
        /// Creates the objectives for the round players. This base method assigns a single objective for each player.
        /// </summary>
        protected virtual void CreateObjectives()
        {
            EntitySpawnSystem entitySpawnSystem = SystemLocator.Get<EntitySpawnSystem>();
            List<PlayerControllable> playersToAssign = entitySpawnSystem.SpawnedPlayers;

            PossibleObjectives = PossibleObjectives.Clone();
            int objectivesCount = PossibleObjectives.Count;

            // Attributes objectives to players while we still have players.
            while (playersToAssign.Count != 0)
            {
                int randomObjectiveIndex = Random.Range(0, objectivesCount);

                PossibleObjectives.TryGetAt(randomObjectiveIndex, out GamemodeObjective objective);
                PlayerControllable player = playersToAssign.First();

                playersToAssign.RemoveAt(0);

                CreateAndAssignObjective(objective, player);
            }
        }

        protected virtual void CreateAndAssignObjective(GamemodeObjective objective, PlayerControllable player)
        {
            objective.SetAssignee(player.ControllingSoul.Owner);
            objective.OnGamemodeObjectiveUpdated += HandleGamemodeObjectiveUpdated;

            objective.InitializeObjective();
        }

        /// <summary>
        /// Fails all objectives once the round ends.
        /// </summary>
        public void FailOnGoingObjectives()
        {
            foreach (GamemodeObjective objective in _roundObjectives)
            {
                if (objective.InProgress)
                {
                    objective.Fail();
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
