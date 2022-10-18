namespace SS3D.Interactions
{
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