using SS3D.Interactions.Interfaces;

namespace SS3D.Interactions
{
    public class ClientInteractionInstance
    {
        public ClientInteractionInstance(IClientInteraction interaction, InteractionEvent interactionEvent, InteractionReference reference)
        {
            Interaction = interaction;
            Event = interactionEvent;
            Reference = reference;
        }

        public IClientInteraction Interaction { get; }
        public InteractionEvent Event { get; }
        public InteractionReference Reference { get; }
        public bool FirstTick { get; set; } = true;
    }
}