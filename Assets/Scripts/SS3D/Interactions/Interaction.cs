using System;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Represents an interaction that can be performed
    /// </summary>
    public class Interaction : IInteraction
    {
        public Sprite Icon;

        /// <summary>
        /// Creates a client interaction (client-side)
        /// </summary>
        public virtual IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return new ClientDelayedInteraction();
        }

        public virtual string GetGenericName()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the name when interacted with a source
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <returns>The display name of the interaction</returns>
        public virtual string GetName(InteractionEvent interactionEvent)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the interaction icon
        /// </summary>
        public virtual Sprite GetIcon(InteractionEvent interactionEvent)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks if this interaction can be executed
        /// </summary>
        /// <param name="interactionEvent">The interaction source</param>
        /// <returns>If the interaction can be executed</returns>
        public virtual bool CanInteract(InteractionEvent interactionEvent)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts the interaction (server-side)
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <param name="reference"></param>
        /// <returns>If the interaction should continue running</returns>
        public virtual bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Continues the interaction (server-side)
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <param name="reference"></param>
        /// <returns>If the interaction should continue running</returns>
        public virtual bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            return true;
        }

        /// <summary>
        /// Called when the interaction is cancelled (server-side)
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <param name="reference"></param>
        public virtual void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            return;
        }
    }
}