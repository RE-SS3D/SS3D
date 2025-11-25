using Coimbra;
using JetBrains.Annotations;
using SS3D.Logging;
using SS3D.Systems.Gamemodes;
using SS3D.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SS3D.Systems.GameModes.Modes
{
    /// <summary>
    /// Executes all gamemode related business. Manipulates the gamemode objectives.
    /// All that is run here is server-only.
    /// </summary>
    [CreateAssetMenu(menuName = "Gamemode/Modes/Gamemode", fileName = "NewGamemode")]
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
        /// Called whenever a gamemode objective is updated, used for quick access by the GamemodeSubSystem.
        /// </summary>
        public event Action<GamemodeObjective> OnObjectiveUpdated;

        /// <summary>
        /// The next objective identifier to be allocated
        /// </summary>
        private int _nextObjectiveId;

        /// <summary>
        /// All the antagonists spawned in the round.
        /// </summary>
        public List<string> RoundAntagonists { get; private set; }

        /// <summary>
        /// The objectives in the current round.
        /// </summary>
        public List<GamemodeObjective> RoundObjectives { get; private set; }

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// All possible objective in the round.
        /// </summary>
        [field:SerializeField]
        public GamemodeObjectiveCollection PossibleObjectives { get; private set; }

        /// <summary>
        /// The display name of this gamemode.
        /// </summary>
        [field:SerializeField]
        public string GamemodeName { get; private set; }

        [NotNull]
        public static Gamemode CreateInstance(GamemodeObjectiveCollection possibleObjectives)
        {
            Gamemode data = ScriptableObject.CreateInstance<Gamemode>();
            data.Init(possibleObjectives);
            return data;
        }

        /// <summary>
        /// Gets the objectives for a specific player.
        /// </summary>
        /// <param name="assignee">The player whose objectives will be retrieved.</param>
        /// <returns></returns>
        public List<GamemodeObjective> GetPlayerObjectives(string assigneeCkey) => RoundObjectives?.Where(objective => objective.AssigneeCkey == assigneeCkey).ToList();

        /// <summary>
        /// Initializes the gamemode, it is virtual so custom initialization is possible.
        /// </summary>
        /// <param name="spawnedPlayersCkeys">List of Ckeys for all players spawning at initialization.</param>
        public virtual void InitializeGamemode(List<string> spawnedPlayersCkeys)
        {
            RoundAntagonists = new List<string>();
            RoundObjectives = new List<GamemodeObjective>();

            CreateObjectives(spawnedPlayersCkeys);

            IsInitialized = true;
            OnInitialized?.Invoke();
        }

        /// <summary>
        /// Finishes the gamemode, it is virtual, so custom finishing is possible.
        /// </summary>
        public virtual void FinalizeGamemode()
        {
            FinalizeObjectives();

            IsInitialized = false;
            OnFinished?.Invoke(RoundObjectives);
        }

        public void ResetGamemode()
        {
            DestroyAllObjectives();
        }

        /// <summary>
        /// Creates an objective for a player that joined after the round started.
        /// </summary>
        /// <param name="player">The player to assign the objective to.</param>
        public virtual void CreateLateJoinObjective(string playerCkey)
        {
            GamemodeObjective objective = GetRandomObjective();
            AddObjectiveToRoundObjectives(_nextObjectiveId, objective);
            AssignObjective(objective, playerCkey);
            _nextObjectiveId++;
        }

        /// <summary>
        /// Ensures that newly created objectives for a late join player have their listeners set.
        /// </summary>
        /// <param name="playerCkey">The player that has late joined</param>
        public void AddEventListenersForLateJoinObjectives(string playerCkey)
        {
            List<GamemodeObjective> playerObjectives = GetPlayerObjectives(playerCkey);
            foreach (GamemodeObjective objective in playerObjectives)
            {
                objective.AddEventListeners();
            }
        }

        /// <summary>
        /// Creates the objectives for the round players. This base method assigns a single objective for each player.
        /// </summary>
        protected virtual void CreateObjectives(List<string> spawnedPlayersCkeys)
        {
            Log.Information(this, "Creating initial objectives", Logs.ServerOnly);

            // Determine the minimum number of assignees (so every gets an objective)
            int numberOfPlayers = spawnedPlayersCkeys.Count;

            // Populate a list of objective entries
            CreateRoundStartObjectives(numberOfPlayers);

            // Assign players to those objective entries
            AssignRoundStartObjectives(spawnedPlayersCkeys);
        }

        /// <summary>
        /// Very basic implementation of objective assignment. This method simply assigns
        /// pre-generated objectives sequentially to the list of Ckeys. Can be overridden for
        /// custom objective assignment.
        /// </summary>
        /// <param name="ckeysToAssign">List of Ckeys to assign objectives to</param>
        protected virtual void AssignRoundStartObjectives(List<string> ckeysToAssign)
        {
            // Validate that input is correct.
            if (ckeysToAssign.Count != RoundObjectives.Count)
            {
                Log.Error(
                    this,
                    "Number of objective entries ({RoundObjectivesCount}) and players ({ckeysCount}) do not match!",
                    Logs.Generic,
                    RoundObjectives.Count,
                    ckeysToAssign.Count);
            }

            // Sequentially assign objectives
            for (int i = 0; i < ckeysToAssign.Count; i++)
            {
                AssignObjective(RoundObjectives[i], ckeysToAssign[i]);
            }
        }

        /// <summary>
        /// Finalizes all objectives in the round. Checks their CheckCompletion,
        /// because sometimes an item has to be in the hand of some player for an objective to be completed.
        /// </summary>
        protected virtual void FinalizeObjectives()
        {
            CheckAllObjectivesCompletion();
            FailOnGoingObjectives();

            int succeededObjectives = RoundObjectives.Count(objective => objective.Succeeded);
            Log.Information(
                this,
                "Objectives Completed: {succeededObjectives}/{roundObjectivesCount}",
                Logs.Generic,
                succeededObjectives,
                RoundObjectives.Count);
        }

        /// <summary>
        /// Gets random objective, IMPORTANT: makes sure the PossiblesObjectives were cloned before calling this.
        /// </summary>
        /// <returns>A random objective and its index in the PossibleObjectives list.</returns>
        protected virtual GamemodeObjective GetRandomObjective()
        {
            int objectivesCount = PossibleObjectives.Count;
            int randomObjectiveIndex = Random.Range(0, objectivesCount);

            PossibleObjectives.TryGetAt(randomObjectiveIndex, out GamemodeObjective objective);

            return objective;
        }

        /// <summary>
        /// Creates and assigns an objective to a player.
        /// </summary>
        /// <param name="objective">The objective to assign a player to.</param>
        /// <param name="player">The player to be assigned to the objective</param>
        protected virtual void AssignObjective(GamemodeObjective objective, string playerCkey)
        {
            objective.SetAssignee(playerCkey);

            string title = $"[{objective.Id}/{objective.Title}]";
            string playerName = $"[{playerCkey}]".Colorize(LogColors.Blue);

            Log.Information(this, "Objective initialized {title} for {playerName}", Logs.ServerOnly, title, playerName);
        }

        private void CheckAllObjectivesCompletion()
        {
            foreach (GamemodeObjective objective in RoundObjectives)
            {
                objective.CheckCompletion();

                ObjectiveStatus status = objective.Status;

                Log.Information(this, "{objectiveTitle} - {status}", Logs.Generic, objective.Title, status);
            }
        }

        private void DestroyAllObjectives()
        {
            foreach (GamemodeObjective gamemodeObjective in RoundObjectives)
            {
                gamemodeObjective.Dispose(true);
            }

            RoundObjectives.Clear();
        }

        private void Init(GamemodeObjectiveCollection possibleObjectives)
        {
            PossibleObjectives = possibleObjectives;
        }

        private void CreateRoundStartObjectives(int amountToCreate)
        {
            // Clones the possible objectives list, to not alter the base file.
            PossibleObjectives = PossibleObjectives.Clone();

            // Create all the objectives
            while (RoundObjectives.Count < amountToCreate)
            {
                // Get a possible objective
                GamemodeObjective objective = GetRandomObjective();

                // Check validity of that objective
                bool validObjective = true;
                if (objective == null)
                {
                    // No objective was returned
                    validObjective = false;
                }
                else if (objective.MinAssignees > amountToCreate - RoundObjectives.Count)
                {
                    // We don't have enough players needing objectives to meet min player requirement.
                    validObjective = false;
                }

                // If it is valid, put an entry into RoundObjectives for each player who will be assigned.
                if (!validObjective)
                {
                    continue;
                }

                int listEntries = Math.Min(objective.MaxAssignees, amountToCreate - RoundObjectives.Count);
                for (int i = 0; i < listEntries; i++)
                {
                    // Duplicate the objective so that we aren't simply reassigning the same one.
                    GamemodeObjective duplicateObjective = ObjectiveFactory.Duplicate(objective);

                    // Add it to the round objectives list
                    AddObjectiveToRoundObjectives(_nextObjectiveId, duplicateObjective);
                }

                _nextObjectiveId++;
            }
        }

        private void AddObjectiveToRoundObjectives(int objectiveId, GamemodeObjective objective)
        {
            objective.SetId(objectiveId);
            objective.InitializeObjective();
            objective.OnGamemodeObjectiveUpdated += HandleGamemodeObjectiveUpdated;
            RoundObjectives.Add(objective);
        }

        /// <summary>
        /// Fails all objectives once the round ends.
        /// </summary>
        private void FailOnGoingObjectives()
        {
            foreach (GamemodeObjective objective in RoundObjectives)
            {
                if (objective.InProgress)
                {
                    objective.Fail();
                }
            }
        }

        /// <summary>
        /// Called whenever an objective is updated to tell the GamemodeSubSystem an objective was updated.
        /// </summary>
        /// <param name="objective">The objective that was updated.</param>
        private void HandleGamemodeObjectiveUpdated(GamemodeObjective objective)
        {
            FinalizeSharedObjective(objective);
            OnObjectiveUpdated?.Invoke(objective);
        }

        /// <summary>
        /// Finalizes any objective that is shared with other players (whether co-opererative or competitive)
        /// once one of the players has triggered completion.
        /// </summary>
        /// <param name="triggerObjective">The objective that was completed</param>
        private void FinalizeSharedObjective(GamemodeObjective triggerObjective)
        {
            // Finalization is only valid if the trigger objective has succeeded, and it is a shared objective. Exit early if invalid.
            if (triggerObjective.CollaborationType == CollaborationType.Individual || !triggerObjective.Succeeded)
            {
                return;
            }

            // Find all other objectives that need to be resolved. Exit early if there are none.
            List<GamemodeObjective> sharedObjectives = RoundObjectives?.Where(sharedObjective => sharedObjective.Id == triggerObjective.Id).ToList();

            if (sharedObjectives == null || sharedObjectives.Count == 0)
            {
                return;
            }

            // Determine the status of remaining participants
            ObjectiveStatus statusOfRemainingParticipants;
            if (triggerObjective.CollaborationType == CollaborationType.Cooperative)
            {
                statusOfRemainingParticipants = ObjectiveStatus.Success;
            }
            else
            {
                statusOfRemainingParticipants = ObjectiveStatus.Failed;
            }

            // Set the status of remaining participants
            foreach (GamemodeObjective objective in sharedObjectives)
            {
                // Only set the status if In Progress - some may have been cancelled etc.
                if (objective.InProgress)
                {
                    objective.SetStatus(statusOfRemainingParticipants);
                }
            }
        }
    }
}
