using UnityEngine;

namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Represents an interaction that can be performed
    /// </summary>
    public interface IInteraction
    {
        /// <summary>
        /// Gets the name when interacted with a source
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <returns>The display name of the interaction</returns>
        string GetName(InteractionEvent interactionEvent);

        /// <summary>
        /// Get the stable wire identifier for this interaction. Must not depend on runtime toggle or animation state.
        /// Display text belongs in <see cref="GetName"/>.
        /// </summary>
        string GetGenericName();

        /// <summary>
        /// Gets the interaction icon
        /// </summary>
        Sprite GetIcon(InteractionEvent interactionEvent);

        /// <summary>
        /// Relative ordering for primary-click selection and radial menus. Higher runs first.
        /// </summary>
        int Priority => 0;

        /// <summary>
        /// Checks if this interaction can be executed
        /// </summary>
        /// <param name="interactionEvent">The interaction source</param>
        /// <returns>If the interaction can be executed</returns>
        bool CanInteract(InteractionEvent interactionEvent);

        /// <summary>
        /// Starts the interaction (server-side)
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <param name="reference"></param>
        /// <returns>If the interaction should continue running</returns>
        bool Start(InteractionEvent interactionEvent, InteractionReference reference);
    }
}