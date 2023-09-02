using System;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    // a drop interaction is when we remove an item from the hand
    [Serializable]
    public class DropInteraction : Interaction
    {
        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Drop";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : Assets.Get(InteractionIcons.Discard);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // if the interaction source's parent is not a hand we return false
            if (interactionEvent.Source.GetRootSource() is not Hand)
            {
                return false;
            }

            // and we do a range check just in case
            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            // we check if the source of the interaction is a hand
            if (interactionEvent.Source.GetRootSource() is Hand hand)
            {
                // we place the item in the hand in the point we clicked
                hand?.PlaceHeldItemOutOfHand(interactionEvent.Point, hand.ItemInHand.transform.rotation);
            }

            return false;
        }
    }
}