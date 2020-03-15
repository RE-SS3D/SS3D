using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Mirror;

namespace Engine.Interactions
{
    /**
     * <summary>An advanced form of interaction can produce multiple possible Interactions</summary>
     * <remarks>
     * TODO: Once Unity has C# 8 default interface implementations, Interaction and ContinuousInteraction could be made sub-interfaces of this.
     * </remarks>
     * TODO: This is not used yet, just here for demonstration
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
        public Action<InteractionEvent> interact;
        public string name;

        public InteractionEvent Event { get; set; }
        public string Name => name;

        public bool CanInteract() => true;
        public void Interact() => interact.Invoke(Event);
    }
    /**
     * <summary>Allow creating of a ContinuousInteraction inline</summary>
     */
    public class ContinuousInteractionInstance : ContinuousInteraction
    {
        // Used for that special { name = ... } construction
        public string name;
        public Action<InteractionEvent> beginInteraction;
        public Func<InteractionEvent, bool> continueInteraction;
        public Action endInteraction;

        public InteractionEvent Event { get; set; }
        public string Name => name;

        // Assume if it's being returned in the Generate list, it's ok.
        public bool CanInteract() => true;

        public void Interact() => beginInteraction.Invoke(Event);
        public bool ContinueInteracting() => continueInteraction.Invoke(Event);
        public void EndInteraction() => endInteraction.Invoke();
    }
}
