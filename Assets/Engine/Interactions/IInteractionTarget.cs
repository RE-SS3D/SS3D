using SS3D.Engine.Examine;

namespace SS3D.Engine.Interactions
{
    /// <summary>
    /// A target which can be interacted with
    /// </summary>
    public interface IInteractionTarget : ISelectable
    {
        /// <summary>
        /// Generates possible interactions (not checked for CanExecute)
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <returns>All possible interactions</returns>
        IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent);
    }
}