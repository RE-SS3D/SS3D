using UnityEngine;
using System.Collections;

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
    public abstract class ContinuousInteractionSO : ScriptableObject, ContinuousInteraction
    {
        public virtual InteractionEvent Event { get; set; }
        public virtual string Name => name;

        public abstract bool CanInteract();

        public abstract void Interact();
        public abstract bool ContinueInteracting();
        public abstract void EndInteraction();
    }
}