using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Inventory.Interactions
{
    // This Interaction takes the first available item inside a container
    public sealed class TakeFirstInteraction : Interaction
    {
        private readonly AttachedContainer _attachedContainer;

        public TakeFirstInteraction(AttachedContainer attachedContainer)
        {
            _attachedContainer = attachedContainer;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Take in " + _attachedContainer.ContainerName;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : Icons.Get<Sprite>(InteractionIcons.Take);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            // Will only appear if the current hand is empty and the container isn't empty
            if (interactionEvent.Source is Hand hand && _attachedContainer != null)
            {
                return hand.IsEmpty() && !_attachedContainer.Empty;
            }

            return false;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hand hand = (Hand) interactionEvent.Source;

            Item pickupItem = _attachedContainer.Items.First();

            if (pickupItem != null)
            {
                hand.Pickup(pickupItem);
            }

            return false;
        }
    }
}