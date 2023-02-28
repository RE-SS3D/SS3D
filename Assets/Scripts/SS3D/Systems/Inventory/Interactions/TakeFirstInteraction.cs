using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    // This Interaction takes the first available item inside a container
    public sealed class TakeFirstInteraction : Interaction
    {
        private readonly ContainerDescriptor _containerDescriptor;

        public TakeFirstInteraction(ContainerDescriptor containerDescriptor)
        {
            _containerDescriptor = containerDescriptor;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Take in " + _containerDescriptor.ContainerName;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : Assets.Get(InteractionIcons.Take);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            // Will only appear if the current hand is empty and the container isn't empty
            AttachedContainer target = _containerDescriptor.AttachedContainer;
            if (interactionEvent.Source is Hands hands && target != null)
            {
                return hands.SelectedHandEmpty && !target.Container.Empty;
            }

            return false;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hands hands = (Hands) interactionEvent.Source;
            int index = _containerDescriptor.AttachedContainer.Container.StoredItems.Count - 1;
            Item pickupItem = _containerDescriptor.AttachedContainer.Container.StoredItems[index].Item;
            if (pickupItem != null)
            {
                hands.Pickup(pickupItem);
            }
            return false;
        }
    }
}