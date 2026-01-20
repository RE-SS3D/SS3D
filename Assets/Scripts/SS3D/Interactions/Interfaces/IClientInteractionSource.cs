namespace SS3D.Interactions.Interfaces
{
    public interface IClientInteractionSource
    {
        /// <summary>
        /// Creates a client interaction (client-side)
        /// </summary>
        IClientInteraction CreateClient(InteractionEvent interactionEvent) => new ClientDelayedInteraction();
    }
}