using UnityEngine;
using System.Collections;
using Mirror;

namespace Interactions2.Core
{
    /**
     * <summary>
     * An interaction which is attached as a component to a tool or target.
     * <br/> 
     * If attached to an item, may be in use when item is tool or when item is target.
     * </summary>
     * <inheritdoc cref="Interaction" />
     */
    public abstract class InteractionComponent : MonoBehaviour, Interaction
    {
        // TODO: Determine whether here and in InteractionSO there should be a CallableAs: Tool, Target, or Either.

        public NetworkConnection ConnectionToClient { get; set; }

        public abstract bool CanInteract(GameObject tool, GameObject target, RaycastHit at);

        public abstract void Interact(GameObject tool, GameObject target, RaycastHit at);
    }
}
