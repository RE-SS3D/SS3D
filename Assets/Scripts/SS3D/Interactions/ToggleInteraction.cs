using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using System;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Utility class for toggle interactions
    /// </summary>
    public class ToggleInteraction : IInteraction, IClientInteractionSource
    {
        public string Name;
        public Sprite Icon;
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

        public string GetName(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is IToggleable toggle)
            {
                return toggle.GetState() ? OnName : OffName;
            }

            return null;
        }

        public string GetGenericName() => "Toggle";

        /// <summary>
        /// World device toggles (generators, switches) sit above inventory fallbacks but below machine UI panels.
        /// </summary>
        public int Priority => 40;

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            Sprite stateIcon = null;
            if (interactionEvent.Target is IToggleable toggle)
            {
                stateIcon = toggle.GetState() ? IconOn : IconOff;
            }

            if (stateIcon)
            {
                return stateIcon;
            }

            return Icon ? Icon : InteractionIconLookup.Power;
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
    }
}