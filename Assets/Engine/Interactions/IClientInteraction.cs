namespace SS3D.Engine.Interactions
{
    public interface IClientInteraction
    {
        /// <summary>
        /// Starts the interaction
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        bool ClientStart(InteractionEvent interactionEvent);
        /// <summary>
        /// Continues the interaction
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        bool ClientUpdate(InteractionEvent interactionEvent);
        /// <summary>
        /// Called when the interaction is cancelled
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        void ClientCancel(InteractionEvent interactionEvent);
    }
}