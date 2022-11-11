using SS3D.Data;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Storage.Containers;
using SS3D.Systems.Storage.Items;
using UnityEngine;

namespace SS3D.Systems.Storage.Interactions
{
    // This Interaction takes the first available item inside a container
    public sealed class TakeFirstInteraction : IInteraction
    {
        public Sprite Icon;
        private readonly ContainerDescriptor _containerDescriptor;

        public TakeFirstInteraction(ContainerDescriptor containerDescriptor)
        {
            _containerDescriptor = containerDescriptor;
        }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return new ClientDelayedInteraction();
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Take in " + _containerDescriptor.ContainerName;
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : Database.Icons.Get(InteractionIcons.Take);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
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

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
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