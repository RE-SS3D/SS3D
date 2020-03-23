using UnityEngine;
using System.Collections;

namespace SS3D.Engine.Interactions
{
    public interface ContinuousInteraction : Interaction
    {
        /**
         * <summary>Continues the interaction's occuring. Will be called immediately after updating <see cref="Interaction.Event"/></summary>
         * <returns>Whether or not the interaction can continue. If not, then EndInteraction is called.</returns>
         */
        bool ContinueInteracting();

        /**
         * <summary>Ends the interaction.</summary>
         * <remarks>
         * The interaction can end when:
         *  - <see cref="ContinueInteracting"/> returns false
         *  - the player releases the action key
         *  - the interaction source is no longer part of the interaction
         *    (e.g. mouse leaves the target, if target is source of interaction)
         * </remarks>
         */
        void EndInteraction();
    }
}
