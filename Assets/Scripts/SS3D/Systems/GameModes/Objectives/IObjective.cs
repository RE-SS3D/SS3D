using Coimbra.Services.Events;

namespace SS3D.Systems.GameModes.Objectives
{
    public interface IObjective
    {
        public string Title { get; set; }
        public ObjectiveStatus Status { get; set; }
        IEvent SuccessEvent { get; set; }
        IEvent FailEvent { get; set; }

        public string GetTitle()
        {
            return Title;
        }

        public bool Done()
        {
            return (Status != ObjectiveStatus.InProgress);
        }

        public void InitializeObjective();

        public bool Success();
        public bool Fail();
    }
}
