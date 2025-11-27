using JetBrains.Annotations;
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
        [field:SerializeField]
        public GamemodeObjective GamemodeObjective { get; private set; }

        /// <summary>
        /// The probability of a player being assigned to this objective.
        /// </summary>
        [field:SerializeField]
        public float AssignmentProbability { get; private set; }

        /// <summary>
        /// How many objectives of this type are remaining to be assigned.
        /// </summary>
        [field:SerializeField]
        public int RemainingAssignments { get; private set; }

        // TODO: Job restrictions 🤯🥸
        [NotNull]
        public static GamemodeObjectiveCollectionEntry CreateInstance(GamemodeObjective objective, float assignmentProbability, int remainingAssignments)
        {
            GamemodeObjectiveCollectionEntry data = ScriptableObject.CreateInstance<GamemodeObjectiveCollectionEntry>();
            data.Init(objective, assignmentProbability, remainingAssignments);
            return data;
        }

        /// <summary>
        /// Tries to get an objective, instantiates it instead of getting the original.
        /// </summary>
        /// <param name="objective">The objective received.</param>
        /// <param name="useRestrictions">Use the remaining assignments and prevent getting objectives when none are remaining.</param>
        /// <returns></returns>
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

        private void Init(GamemodeObjective objective, float assignmentProbability, int remainingAssignments)
        {
            GamemodeObjective = objective;
            AssignmentProbability = assignmentProbability;
            RemainingAssignments = remainingAssignments;
        }
    }
}
