using System;
using FishNet.Connection;
using UnityEngine;

namespace SS3D.Systems.Gamemodes
{
    public class GamemodeObjective : ScriptableObject, IGamemodeObjective
    {
        public event Action<GamemodeObjective> OnGamemodeObjectiveUpdated; 

        public int Id { get; set; }
        public string Title { get; set; }
        public ObjectiveStatus Status { get; set; }
        public NetworkConnection Author { get; set; }

        public GamemodeObjective(int id, string title, ObjectiveStatus status, NetworkConnection author)
        {
            Id = id;
            Title = title;
            Status = status;
            Author = author;
        }

        public virtual void InitializeObjective()
        {
            SetStatus(ObjectiveStatus.InProgress);
        }
        public virtual void FinalizeObjective() { }

        protected void Succeed()
        {
            if (Status != ObjectiveStatus.InProgress)
            {
                return;
            }

            SetStatus(ObjectiveStatus.Success);
        }

        public void Fail()
        {
            if (Status != ObjectiveStatus.InProgress)
            {
                return;
            }

            SetStatus(ObjectiveStatus.Failed);
            
        }

        public void SetStatus(ObjectiveStatus status)
        {
            Status = status;
            OnGamemodeObjectiveUpdated?.Invoke(this);
        }

        public void SetAuthor(NetworkConnection author)
        {
            Author = author;
            OnGamemodeObjectiveUpdated?.Invoke(this);
        }
    }
}