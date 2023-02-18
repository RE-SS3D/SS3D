using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Items.Generic
{
    /// <summary>
    /// Honks a horn. Honking requires the target to be BikeHorn
    /// </summary>
    public class HonkInteraction : Interaction
    {
        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Honk";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : Assets.Get(InteractionIcons.Honk);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            bool inRange = InteractionExtensions.RangeCheck(interactionEvent);

            if (target is not BikeHorn horn)
            {
                return false;
            }

            if (!inRange)
            {
                return false;
            }

            return !horn.IsHonking();
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is BikeHorn horn)
            {
                horn.Honk();
            }
            return false;
        }
    }
}