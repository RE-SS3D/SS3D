using System;

namespace SS3D.Systems.GameModes.Objectives
{
    [Serializable]
    public struct GamemodeObjectiveData
    {
        public string Title;
        public ObjectiveStatus Status;

        public GamemodeObjectiveData(GamemodeObjective objective)
        {
            Title = objective.Title;
            Status = objective.Status;
        }

        public GamemodeObjectiveData(string title, ObjectiveStatus status)
        {
            Title = title;
            Status = status;
        }
    }
}