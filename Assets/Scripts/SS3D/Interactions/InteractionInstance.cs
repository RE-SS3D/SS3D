using FishNet.Connection;
using SS3D.Interactions.Interfaces;

namespace SS3D.Interactions
{
    /// <summary>
    /// The instance of an interaction in progress
    /// </summary>
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
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((InteractionInstance) obj);
        }

        public override int GetHashCode()
        {
            return (Reference != null ? Reference.GetHashCode() : 0);
        }
    }
}