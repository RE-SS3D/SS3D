using System;
using FishNet.Connection;
using UnityEngine;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// An objective in the gamemode.
    /// </summary>
    public class GamemodeObjective : ScriptableObject, IGamemodeObjective
    {
        public event Action<GamemodeObjective> OnGamemodeObjectiveUpdated;

        /// <inheritdoc />
        public int Id { get; set; }

        /// <inheritdoc />
        public string Title { get; set; }

        /// <inheritdoc />
        public ObjectiveStatus Status { get; set; }

        /// <inheritdoc />
        public NetworkConnection Author { get; set; }

        public GamemodeObjective(int id, string title, ObjectiveStatus status, NetworkConnection author)
        {
            Id = id;
            Title = title;
            Status = status;
            Author = author;
        }

        /// <inheritdoc />
        public virtual void InitializeObjective()
        {
            SetStatus(ObjectiveStatus.InProgress);
        }

        /// <inheritdoc />
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