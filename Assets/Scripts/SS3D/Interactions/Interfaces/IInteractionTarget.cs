﻿namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// A target which can be interacted with
    /// </summary>
    public interface IInteractionTarget
    {
        /// <summary>
        /// Creates possible interactions (not checked for CanExecute)
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <returns>All created interactions</returns>
        IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent);
    }
}