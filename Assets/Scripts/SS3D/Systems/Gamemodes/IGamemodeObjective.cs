using FishNet.Connection;

namespace SS3D.Systems.Gamemodes
{
    public interface IGamemodeObjective
    {
        /// <summary>
        /// Unique ID used for instantiated objectives, the Gamemode object takes care of that.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The title of the objective, IE:"Steal the HOS jacket".
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The completion status of that objective.
        /// </summary>
        public ObjectiveStatus Status { get; set; }
        /// <summary>
        /// Who is in charge of completing this objective.
        /// </summary>
        public NetworkConnection Author { get; set; }

        /// <summary>
        /// Initializes this objective.
        /// </summary>
        public void InitializeObjective() {}

        /// <summary>
        /// Finalizes this objective.
        /// </summary>
        public void FinalizeObjective() {}

        /// <summary>
        /// Changes the completion state to Success
        /// </summary>
        public void Succeed() {}

        /// <summary>
        /// Changes the completion state to Failure.
        /// </summary>
        public void Fail() {}
    }
}