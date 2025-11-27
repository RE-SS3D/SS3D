using SS3D.Interactions;
using SS3D.Interactions.Interfaces;

namespace SS3D.Systems.Interactions
{
    public struct InteractionEntryAndEvents
    {
        public readonly IInteractionTarget Target;
        public readonly IInteraction Interaction;
        public readonly InteractionEvent Event;

        public InteractionEntryAndEvents(IInteractionTarget target, IInteraction interaction, InteractionEvent interactionEvent)
        {
            Target = target;
            Event = interactionEvent;
            Interaction = interaction;
        }
    }
}
