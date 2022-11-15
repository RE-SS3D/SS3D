using Coimbra.Services.Events;
using SS3D.Systems.GameModes.Objectives;

namespace SS3D.Systems.GameModes.Events
{
    public partial struct ObjectiveStatusChangedEvent : IEvent
    {
        public readonly GamemodeObjective Objective;

        public ObjectiveStatusChangedEvent(GamemodeObjective objective)
        {
            Objective = objective;
        }
    }
}