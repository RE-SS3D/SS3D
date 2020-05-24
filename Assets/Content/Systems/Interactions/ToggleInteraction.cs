using System;
using SS3D.Engine.Interactions;

namespace SS3D.Content.Systems.Interactions
{
    /// <summary>
    /// Utility class for toggle interactions
    /// </summary>
    public class ToggleInteraction : IInteraction
    {
        /// <summary>
        /// Checks if the interaction should be possible
        /// </summary>
        public Predicate<InteractionEvent> CanInteractCallback { get; set; } = _ => true;
        /// <summary>
        /// The name for the interaction when state is true
        /// </summary>
        public string OnName { get; set; }
        /// <summary>
        /// The name for the interaction when state is false
        /// </summary>
        public string OffName { get; set; }
        
        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is IToggleable toggle1)
            {
                return toggle1.GetState() ? OnName : OffName;
            }
            if (interactionEvent.Source is IToggleable toggle)
            {
                return toggle.GetState() ? OnName : OffName;
            } 

            return null;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            return CanInteractCallback.Invoke(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is IToggleable toggle1)
            {
                toggle1.Toggle();
            }
            else if (interactionEvent.Source is IToggleable toggle)
            {
                toggle.Toggle();
            }

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }
    }
}