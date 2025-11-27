using SS3D.Interactions.Interfaces;

namespace SS3D.Interactions
{
    /// <summary>
    /// An entry of an interaction and it's target interactable
    /// </summary>
    public struct InteractionEntry
    {
        public readonly IInteractionTarget Target;
        public readonly IInteraction Interaction;

        public InteractionEntry(IInteractionTarget target, IInteraction interaction)
        {
            Target = target;
            Interaction = interaction;
        }
    }
}