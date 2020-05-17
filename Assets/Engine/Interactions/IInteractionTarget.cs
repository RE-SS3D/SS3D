namespace SS3D.Engine.Interactions
{
    /// <summary>
    /// A target which can be interacted with
    /// </summary>
    public interface IInteractionTarget
    {
        /// <summary>
        /// Generates possible interactions (not checked for CanExecute)
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <returns>All possible interactions</returns>
        IInteraction[] GenerateInteractions(InteractionEvent interactionEvent);
    }
}