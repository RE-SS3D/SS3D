namespace SS3D.Interactions.Interfaces
{
    public interface IDelayedInteraction : IInteraction
    {
        /// <summary>
        /// Continues the interaction (server-side)
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <param name="reference"></param>
        /// <returns>If the interaction should continue running</returns>
        bool Update(InteractionEvent interactionEvent, InteractionReference reference);

        /// <summary>
        /// Called when the interaction is cancelled (server-side)
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <param name="reference"></param>
        void Cancel(InteractionEvent interactionEvent, InteractionReference reference);
    }
}