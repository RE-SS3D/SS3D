using SS3D.Systems.Entities;
using UnityEngine;

namespace SS3D.Systems.GameModes.Objectives
{
    public abstract class GamemodeObjective : ScriptableObject
    {
        public string Title { get; set; }
        public ObjectiveStatus Status { get; set; }

        public abstract void InitializeObjective();

        public abstract void CheckCompleted();
    }
}