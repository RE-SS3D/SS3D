using FishNet.Connection;

namespace SS3D.Systems.GameModes.Objectives
{
    public interface IGamemodeObjective
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public ObjectiveStatus Status { get; set; }
        public NetworkConnection Author { get; set; }

        public void InitializeObjective() {}
        public void FinalizeObjective() {}
    }
}