using Coimbra.Services.Events;
using SS3D.Systems.GameModes.Objectives;

namespace SS3D.Systems.GameModes.Events
{
    public partial struct SerializedObjectiveStatusChangedEvent : IEvent
    {
        public readonly GamemodeObjective Objective;

        public SerializedObjectiveStatusChangedEvent(GamemodeObjective objective)
        {
            Objective = objective;
        }
    }
}