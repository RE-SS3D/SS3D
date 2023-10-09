using System;
using UnityEngine;

namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Represents an interaction that can be performed
    /// </summary>
    public interface IInteraction
    {
        /// <summary>
        /// Creates a client interaction (client-side)
        /// </summary>
        IClientInteraction CreateClient(InteractionEvent interactionEvent);

        /// <summary>
        /// Gets the name when interacted with a source
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <returns>The display name of the interaction</returns>
        string GetName(InteractionEvent interactionEvent);

        /// <summary>
        /// Get the generic name of interaction. Pickup interaction should return "Pickup".
        /// GetName should return "Pickup item" with item the name of the item to pickup.
        /// </summary>
        /// <returns></returns>
        string GetGenericName();

        /// <summary>
        /// Gets the interaction icon
        /// </summary>
        Sprite GetIcon(InteractionEvent interactionEvent);

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