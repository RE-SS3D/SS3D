using System;
using FishNet.Connection;
using UnityEngine;

namespace SS3D.Systems.Gamemodes
{
    /// <inheritdoc cref="SS3D.Systems.Gamemodes.IGamemodeObjective" />
    public class GamemodeObjective : ScriptableObject, IGamemodeObjective
    {
        /// <summary>
        /// Called whenever an objective is updated.
        /// </summary>
        public event Action<GamemodeObjective> OnGamemodeObjectiveUpdated;

        /// <inheritdoc />
        public int Id { get; set; }

        /// <inheritdoc />
        public string Title { get; set; }

        /// <inheritdoc />
        public ObjectiveStatus Status { get; set; }

        /// <inheritdoc />
        public NetworkConnection Assignee { get; set; }

        /// <summary>
        /// Handy call to check objective success.
        /// </summary>
        public bool Succeeded => Status == ObjectiveStatus.Success;

        /// <summary>
        /// Handy call to check objective failure.
        /// </summary>
        public bool Failed => Status == ObjectiveStatus.Failed;

        /// <summary>
        /// Handy call to check objective cancellation.
        /// </summary>
        public bool Cancelled => Status == ObjectiveStatus.Cancelled;

        /// <summary>
        /// Handy call to check if the objective is in progress.
        /// </summary>
        public bool InProgress => Status == ObjectiveStatus.InProgress;

        public GamemodeObjective(int id, string title, ObjectiveStatus status, NetworkConnection author)
        {
            Id = id;
            Title = title;
            Status = status;
            Assignee = author;
        }

        /// <inheritdoc />
        public virtual void InitializeObjective()
        {
            SetStatus(ObjectiveStatus.InProgress);
        }

        /// <inheritdoc />
        public virtual void FinalizeObjective()
        {
            // You could do something here I suppose.
        }

        /// <inheritdoc />
        public virtual void CheckCompletion()
        {
            // Makes sure everything is alright before finalizing the objective.
        }

        /// <inheritdoc />
        public void Succeed()
        {
            if (Status != ObjectiveStatus.InProgress)
            {
                return;
            }

            SetStatus(ObjectiveStatus.Success);
        }

        /// <inheritdoc />
        public void Fail()
        {
            if (Status != ObjectiveStatus.InProgress)
            {
                return;
            }

            SetStatus(ObjectiveStatus.Failed);
        }

        /// <summary>
        /// Updates the status of the gamemode objective. Calls the OnGamemodeObjectiveUpdated event.
        /// </summary>
        /// <param name="status">The new status.</param>
        public void SetStatus(ObjectiveStatus status)
        {
            Status = status;
            OnGamemodeObjectiveUpdated?.Invoke(this);
        }

        /// <summary>
        /// Sets a new author for the gamemode objective. Calls the OnGamemodeObjectiveUpdated event.
        /// </summary>
        /// <param name="assignee">The new assignee.</param>
        public void SetAssignee(NetworkConnection assignee)
        {
            Assignee = assignee;
            OnGamemodeObjectiveUpdated?.Invoke(this);
        }
    }
}