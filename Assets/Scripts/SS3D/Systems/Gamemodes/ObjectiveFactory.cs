using UnityEngine;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// This class is used to create and duplicate GamemodeObjectives so that the underlying ScriptableObjects are not modified.
    /// </summary>
    public static class ObjectiveFactory
    {
        public static GamemodeObjective Duplicate(GamemodeObjective objective)
        {
            GamemodeObjective newObjective = ScriptableObject.Instantiate(objective);
            newObjective.name = objective.name;
            return newObjective;
        }

        public static GamemodeObjective Create(string title, CollaborationType collaborationType, Alignment alignment, int minAssignees, int maxAssignees)
        {
            GamemodeObjective newObjective = new GamemodeObjective(
                title, collaborationType, alignment, minAssignees, maxAssignees);

            return newObjective;
        }
    }
}