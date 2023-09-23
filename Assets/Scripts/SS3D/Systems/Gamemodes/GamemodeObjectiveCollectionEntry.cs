using UnityEngine;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// Used to control GamemodeObjective distribution params.
    /// </summary>
    [CreateAssetMenu(menuName = "Gamemode/GamemodeObjectiveCollectionEntry", fileName = "GamemodeObjectiveCollectionEntry", order = 0)]
    public class GamemodeObjectiveCollectionEntry : ScriptableObject
    {
        /// <summary>
        /// The gamemode objective to be attributed.
        /// </summary>
        public GamemodeObjective GamemodeObjective;

        /// <summary>
        /// The probability of a player being assigned to this objective.
        /// </summary>
        public float AssignmentProbability;

        /// <summary>
        /// How many objectives of this type are remaining to be assigned.
        /// </summary>
        public int RemainingAssignments;

        // TODO: Job restrictions 🤯🥸

        /// <summary>
        /// Tries to get an objective, instantiates it instead of getting the original.
        /// </summary>
        /// <param name="objective">The objective received.</param>
        /// <param name="useRestrictions">Use the remaining assignments and prevent getting objectives when none are remaining.</param>
        public bool TryGetObjective(out GamemodeObjective objective, bool useRestrictions = false)
        {
            if (useRestrictions)
            {
                if (RemainingAssignments == 0)
                {
                    objective = null;
                    return false;
                }

                RemainingAssignments--;
            }

            objective = Instantiate(GamemodeObjective);
            return true;
        }
    }
}