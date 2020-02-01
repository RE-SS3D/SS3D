using UnityEngine;
using System.Collections;

namespace Interactions2.Core
{
    public interface ContinuousInteraction : Interaction
    {
        /**
         * <summary>Continues the interaction's occuring</summary>
         * <returns>Whether or not the interaction can continue. If not, then EndInteraction is called.</returns>
         */
        bool ContinueInteracting();

        /**
         * <summary>Ends the interaction. Can occur either because ContinueInteracting returns false, or because the user has terminated the interaction.</summary>
         */
        void EndInteraction();
    }
}
