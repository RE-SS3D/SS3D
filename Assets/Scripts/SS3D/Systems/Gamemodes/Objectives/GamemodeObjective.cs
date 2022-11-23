using SS3D.Systems.GameModes.Events;
using UnityEngine;

namespace SS3D.Systems.GameModes.Objectives
{
    public abstract class GamemodeObjective : ScriptableObject
    {
        public string Title { get; set; }
        public ObjectiveStatus Status { get; protected set; }

        public abstract void InitializeObjective();

        public abstract void CheckCompleted();

        public void Success()
        {
            if (Status == ObjectiveStatus.InProgress)
            {
                Status = ObjectiveStatus.Success;
                new ObjectiveStatusChangedEvent(this).Invoke(this);
            }
        }

        public void Failed()
        {
            if (Status == ObjectiveStatus.InProgress)
            {
                Status = ObjectiveStatus.Failed;
                new ObjectiveStatusChangedEvent(this).Invoke(this);
            }
        }
    }
}