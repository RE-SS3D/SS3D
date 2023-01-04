using System;
using SS3D.Data;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Storage.Containers;
using UnityEngine;

namespace SS3D.Systems.Storage.Interactions
{
    // a drop interaction is when we remove an item from the hand
    [Serializable]
    public class DropInteraction : IInteraction
    {
        public Sprite Icon;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return new ClientDelayedInteraction();
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Drop";
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : AssetData.Get(InteractionIcons.Discard);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
	        // if the interaction source's parent is not a hand we return false
            if (interactionEvent.Source is not Hands)
            {
                return false;
            }

            // and we do a range check just in case
            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
	        // we check if the source of the interaction is a hand
            if (interactionEvent.Source is Hands hands)
            {
		        // we place the item in the hand in the point we clicked
                hands.PlaceHeldItem(interactionEvent.Point, hands.ItemInHand.transform.rotation);
            }

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            return true;
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            return;
        }
    }
}