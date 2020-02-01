using UnityEngine;
using System.Collections;
using Mirror;

namespace Interactions2.Core
{
    /**
     * <summary>
     * An interaction made as an SO
     * <br/> 
     * Should be attached to a Tool or a SpecialTarget.
     * </summary>
     * <inheritdoc cref="Interaction" />
     * 
     * TODO: When we switch to 2019.3, we can make and attach scripts which extend directly from
     *       Interaction, and attach them, rather than extending ScriptableObject and having to make an asset instance
     */
    public abstract class InteractionSO : ScriptableObject, Interaction
    {
        public NetworkConnection ConnectionToClient { get; set; }

        public abstract bool CanInteract(GameObject tool, GameObject target, RaycastHit at);

        public abstract void Interact(GameObject tool, GameObject target, RaycastHit at);
    }
}
