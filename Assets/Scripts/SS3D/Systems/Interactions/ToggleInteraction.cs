using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
// using SS3D.Systems.Animations;
// using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;
using UnityEngine.XR;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Utility class for toggle interactions
    /// </summary>
    public class ToggleInteraction : DelayedInteraction
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

        public override InteractionType InteractionType => InteractionType.Press;

        public ToggleInteraction()
        {
            Delay = 0.2f;
        }

        public override string GetGenericName()
        {
            return "Toggle";
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is IToggleable toggle)
            {
                return toggle.GetState() ? OnName : OffName;
            }

            return null;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is IToggleable toggle)
            {
                return toggle.GetState() ? IconOn : IconOff;
            }

            return null;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (RangeCheck && !InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }
            return CanInteractCallback.Invoke(interactionEvent);
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {

        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is IToggleable toggle1)
            {
                toggle1.Toggle();
            }
            else if (interactionEvent.Source is IToggleable toggle)
            {
                toggle.Toggle();
            }
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Vector3 point = interactionEvent.Point;

            if (interactionEvent.Target.TryGetInteractionPoint(interactionEvent.Source, out Vector3 customPoint))
            {
                point = customPoint;
            }
            
            if (interactionEvent.Source.GetRootSource() is IInteractionSourceAnimate animatedSource)
            {
                animatedSource.PlaySourceAnimation(InteractionType.Press, interactionEvent.Target.GetComponent<NetworkObject>(), point, Delay);
            }

            return true;
        }
    }
}
