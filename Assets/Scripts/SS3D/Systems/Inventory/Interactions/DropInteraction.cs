using JetBrains.Annotations;
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
        private const string GenericDropInteractionName = "Drop";

        private Sprite GenericDropInteractionSprite => Assets.Get(InteractionIcons.Discard);

        [NotNull]
        public override string GetName(InteractionEvent interactionEvent)
        {
            return GenericDropInteractionName;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return IconOverride != null ? IconOverride : GenericDropInteractionSprite;
        }

        public override bool CanInteract([NotNull] InteractionEvent interactionEvent)
        {
	        // if the interaction source's parent is not a hand we return false
            if (interactionEvent.Source.GetRootSource() is not Hands)
            {
                return false;
            }

            // and we do a range check just in case
            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public override bool Start([NotNull] InteractionEvent interactionEvent, InteractionReference reference)
        {
	        // we check if the source of the interaction is a hand
            if (interactionEvent.Source.GetRootSource() is Hands hands)
            {
		        // we place the item in the hand in the point we clicked
                hands.PlaceHeldItem(interactionEvent.Point, hands.ItemInHand.transform.rotation);
            }

            return false;
        }
    }
}