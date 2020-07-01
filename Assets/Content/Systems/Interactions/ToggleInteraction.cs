using System;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    /// <summary>
    /// Utility class for toggle interactions
    /// </summary>
    public class ToggleInteraction : IInteraction
    {
        /// <summary>
        /// The icon when state is true
        /// </summary>
        public Sprite IconOn;
        /// <summary>
        /// The icon when state is false
        /// </summary>
        public Sprite IconOff;
        /// <summary>
        /// Checks if the interaction should be possible
        /// </summary>
        public Predicate<InteractionEvent> CanInteractCallback { get; set; } = _ => true;

        /// <summary>
        /// The name for the interaction when state is true
        /// </summary>
        public string OnName { get; set; } = "Turn off";

        /// <summary>
        /// The name for the interaction when state is false
        /// </summary>
        public string OffName { get; set; } = "Turn on";

        /// <summary>
        /// If the interaction should be range limited
        /// </summary>
        public bool RangeCheck { get; set; } = true;
        
        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is IToggleable toggle)
            {
                return toggle.GetState() ? OnName : OffName;
            }

            return null;
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is IToggleable toggle)
            {
                return toggle.GetState() ? IconOn : IconOff;
            }

            return null;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (RangeCheck && !InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }
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