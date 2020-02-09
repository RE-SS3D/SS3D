using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Mirror;

namespace Interactions2.Core
{
    /**
     * <summary>An advanced form of interaction can produce multiple possible Interactions</summary>
     * <remarks>
     * TODO: Once Unity has C# 8 default interface implementations, Interaction and ContinuousInteraction could be made sub-interfaces of this.
     * </remarks>
     */
    public interface InteractionCreator
    {
        /**
         * <summary>Based on the interaction info, create a list of possible interactions</summary>
         * <returns>A list of interactions</returns>
         */
        List<Interaction> Generate(InteractionEvent e);
    }

    /**
     * <summary>Allow creating of an Interaction inline specifically for an InteractionCreator</summary>
     */
    public class InteractionInstance : Interaction
    {
        // Used for that special { name = ... } construction
        public readonly Action<InteractionEvent> interact;
        public readonly string name;

        public InteractionEvent Event { get; set; }
        public string Name => name;

        public bool CanInteract()
        {
            // Assume if it's being returned in the Generate list, it's ok.
            return true;
        }

        public void Interact()
        {
            interact.Invoke(Event);
        }
    }
    /**
     * <summary>Allow creating of a ContinuousInteraction inline</summary>
     */
    public class ContinuousInteractionInstance : ContinuousInteraction
    {
        // Used for that special { name = ... } construction
        public readonly string name;
        public readonly Action<InteractionEvent> beginInteraction;
        public readonly Func<InteractionEvent, bool> continueInteraction;
        public readonly Action endInteraction;

        public InteractionEvent Event { get; set; }
        public string Name => name;

        public bool CanInteract()
        {
            // Assume if it's being returned in the Generate list, it's ok.
            return true;
        }

        public void Interact()
        {
            beginInteraction.Invoke(Event);
        }
        public bool ContinueInteracting()
        {
            return continueInteraction.Invoke(Event);
        }
        public void EndInteraction()
        {
            endInteraction.Invoke();
        }
    }
}
