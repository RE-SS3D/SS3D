using System;
using SS3D.Engine.Interactions;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    /// <summary>
    /// Utility class for simple interactions
    /// </summary>
    public class SimpleInteraction : IInteraction
    {
        public Sprite icon;
        public string Name { get; set; }
        /// <summary>
        /// Checks if the interaction should be possible
        /// </summary>
        public Predicate<InteractionEvent> CanInteractCallback { get; set; } = _ => true;
        /// <summary>
        /// Executed when the interaction takes place
        /// </summary>
        public Action<InteractionEvent, InteractionReference> Interact { get; set; }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return Name;
        
        }
        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return icon;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            return CanInteractCallback.Invoke(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Interact.Invoke(interactionEvent, reference);
            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new NotImplementedException();
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new NotImplementedException();
        }
    }
}