using System;
using FishNet.Connection;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Systems.Gamemodes
{
    [Serializable]
    /// <inheritdoc cref="SS3D.Systems.Gamemodes.IGamemodeObjective" />
    public class GamemodeObjective : ScriptableObject, IGamemodeObjective
    {
        /// <summary>
        /// Called whenever an objective is updated.
        /// </summary>
        public event Action<GamemodeObjective> OnGamemodeObjectiveUpdated;

        private int _id;
        private string _title;
        private ObjectiveStatus _status;
        private NetworkConnection _assignee;

        /// <inheritdoc />
        public int Id => _id;

        /// <inheritdoc />
        public string Title => _title;

        /// <inheritdoc />
        public ObjectiveStatus Status => _status;

        /// <inheritdoc />
        public NetworkConnection Assignee => _assignee;

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
            _status = status;
            OnGamemodeObjectiveUpdated?.Invoke(this);
        }

        /// <summary>
        /// Sets a new author for the gamemode objective. Calls the OnGamemodeObjectiveUpdated event.
        /// </summary>
        /// <param name="assignee">The new assignee.</param>
        public void SetAssignee(NetworkConnection assignee)
        {
            _assignee = assignee;
            OnGamemodeObjectiveUpdated?.Invoke(this);
        }

        /// <summary>
        /// Sets the new id for this objective.
        /// </summary>
        /// <param name="id"></param>
        public void SetId(int id)
        {
            _id = id;
            OnGamemodeObjectiveUpdated?.Invoke(this);
        }

        public void SetTitle(string title)
        {
            _title = title;
            OnGamemodeObjectiveUpdated?.Invoke(this);
        }
    }
}