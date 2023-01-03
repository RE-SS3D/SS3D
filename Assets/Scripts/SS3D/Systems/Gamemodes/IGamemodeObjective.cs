using FishNet.Connection;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// An objective in the gamemode.
    /// </summary>
    public interface IGamemodeObjective
    {
        /// <summary>
        /// Unique ID used for instantiated objectives, the Gamemode object takes care of that.
        /// </summary>
        public int Id { get; }
        /// <summary>
        /// The title of the objective, IE:"Steal the HOS jacket".
        /// </summary>
        public string Title { get; }
        /// <summary>
        /// The completion status of that objective.
        /// </summary>
        public ObjectiveStatus Status { get; }
        /// <summary>
        /// Who is in charge of completing this objective.
        /// </summary>
        public string AssigneeCkey { get; }

        /// <summary>
        /// Initializes this objective.
        /// </summary>
        public void InitializeObjective() {}

        /// <summary>
        /// Adds any required event listeners.
        /// </summary>
        public void AddEventListeners() { }

        /// <summary>
        /// Finalizes this objective.
        /// </summary>
        public void FinalizeObjective() {}

        /// <summary>
        /// Checks the completion once the round ends.
        /// IE: The objective is the player having an item in the inventory.
        /// </summary>
        public void CheckCompletion() {}

        /// <summary>
        /// Changes the completion state to Success
        /// </summary>
        public void Succeed() {}

        /// <summary>
        /// Changes the completion state to Failure.
        /// </summary>
        public void Fail() {}

        /// <summary>
        /// Changes the completion state to Cancelled.
        /// </summary>
        public void Cancel() { }
    }
}