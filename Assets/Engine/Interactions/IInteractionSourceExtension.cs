using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Interactions
{
    /// <summary>
    /// Allows the creation of additional interactions for an existing source
    /// </summary>
    public interface IInteractionSourceExtension
    {
        /// <summary>
        /// Allows the extension to manipulate existing interactions and add new ones
        /// </summary>
        /// <param name="targets">The interaction targets of this interaction</param>
        /// <param name="interactions">The already present interactions</param>
        void CreateInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions);
    }
}
