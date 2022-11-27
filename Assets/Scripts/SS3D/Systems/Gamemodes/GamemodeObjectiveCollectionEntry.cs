using UnityEngine;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// Used to control GamemodeObjective distribution params.
    /// </summary>
    [CreateAssetMenu(menuName = "GamemodeObjectiveCollectionEntry", fileName = "Gamemode/GamemodeObjectiveCollectionEntry", order = 0)]
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
    }
}