using Mirror;

namespace SS3D.Engine.Interactions
{
    public class InteractionInstance
    {
        public InteractionInstance(IInteraction interaction, InteractionEvent interactionEvent, InteractionReference reference, NetworkConnection owner)
        {
            Interaction = interaction;
            Event = interactionEvent;
            Reference = reference;
            Owner = owner;
        }

        public IInteraction Interaction { get; }
        public InteractionEvent Event { get; }
        public InteractionReference Reference { get; }
        public NetworkConnection Owner { get; }
        public bool FirstTick { get; set; } = true;

        protected bool Equals(InteractionInstance other)
        {
            return Equals(Reference, other.Reference);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InteractionInstance) obj);
        }

        public override int GetHashCode()
        {
            return (Reference != null ? Reference.GetHashCode() : 0);
        }
    }
}