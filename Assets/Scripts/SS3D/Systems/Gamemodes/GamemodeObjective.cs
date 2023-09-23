using System;
using UnityEngine;

namespace SS3D.Systems.Gamemodes
{
    [Serializable]
    public class GamemodeObjective : ScriptableObject, IGamemodeObjective
    {
        /// <summary>
        /// Called whenever an objective is updated.
        /// </summary>
        public event Action<GamemodeObjective> OnGamemodeObjectiveUpdated;

        private int _id;

        [SerializeField]
        private string _title;

        [SerializeField]
        private CollaborationType _collaborationType;

        [SerializeField]
        private Alignment _alignmentRequirement;

        [SerializeField]
        private int _minAssignees = 1;

        [SerializeField]
        private int _maxAssignees = 1;
        private ObjectiveStatus _status;
        private string _assigneeCkey;

        /// <summary>
        /// No-arg constructor
        /// </summary>
        public GamemodeObjective()
        {
        }

        /// <summary>
        /// Constructor allowing creation of specific objective data.
        /// </summary>
        /// <param name="title">Title of the objective (visible to player)</param>
        /// <param name="collaborationType">Whether the objective is individual, competitive or cooperative</param>
        /// <param name="alignment">Whether the objective is valid for antagonists, non-antagonists or both</param>
        /// <param name="minAssignees">Minimum number of players required for this objective</param>
        /// <param name="maxAssignees">Maximum number of players required for this objective</param>
        public GamemodeObjective(string title, CollaborationType collaborationType, Alignment alignment, int minAssignees, int maxAssignees)
        {
            _title = title;
            _collaborationType = collaborationType;
            _alignmentRequirement = alignment;
            _minAssignees = Math.Max(minAssignees, 1);
            _maxAssignees = Math.Max(maxAssignees, _minAssignees);
        }

        public CollaborationType CollaborationType => _collaborationType;

        public Alignment AlignmentRequirement => _alignmentRequirement;

        /// <summary>
        /// The minimum number of assignees for this objective
        /// </summary>
        public int MinAssignees
        {
            get => _minAssignees;
            set => _minAssignees = value;
        }

        /// <summary>
        /// The maximum number of assignees for this objective
        /// </summary>
        public int MaxAssignees
        {
            get => _maxAssignees;
            set => _maxAssignees = value;
        }

        /// <inheritdoc />
        public int Id => _id;

        /// <inheritdoc />
        public string Title => _title;

        /// <inheritdoc />
        public ObjectiveStatus Status => _status;

        /// <inheritdoc />
        public string AssigneeCkey => _assigneeCkey;

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
        public void Cancel()
        {
            if (Status != ObjectiveStatus.InProgress)
            {
                return;
            }

            SetStatus(ObjectiveStatus.Cancelled);
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
        public void SetAssignee(string assigneeCkey)
        {
            _assigneeCkey = assigneeCkey;
            OnGamemodeObjectiveUpdated?.Invoke(this);
        }

        /// <summary>
        /// Sets the new id for this objective.
        /// </summary>
        /// <param name="id"></param>
        public void SetId(int id)
        {
            this._id = id;
            OnGamemodeObjectiveUpdated?.Invoke(this);
        }

        public void SetTitle(string title)
        {
            _title = title;
            OnGamemodeObjectiveUpdated?.Invoke(this);
        }

        /// <inheritdoc />
        public virtual void AddEventListeners() { }
    }
}