using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    // This Interaction takes the first available item inside a container
    public sealed class TakeFirstInteraction : IInteraction
    {
        private readonly AttachedContainer _attachedContainer;

        public TakeFirstInteraction(AttachedContainer attachedContainer, float timeToMoveBackItem, float timeToReachItem)
        {
            TimeToMoveBackItem = timeToMoveBackItem;
            TimeToReachItem = timeToReachItem;
            _attachedContainer = attachedContainer;
        }

        public float TimeToMoveBackItem { get; private set; }

        public float TimeToReachItem { get; private set; }

        public InteractionType InteractionType => InteractionType.None;

        public string GetGenericName() => "Take";

        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        public string GetName(InteractionEvent interactionEvent) => "Take in " + _attachedContainer.ContainerName;

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Take;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            // Will only appear if the current hand is empty and the container isn't empty
            if (interactionEvent.Source is IItemHolder itemHolder && _attachedContainer != null)
            {
                return itemHolder.Empty && !_attachedContainer.Empty;
            }

            return false;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Item pickupItem = _attachedContainer.Items.First();

            if (pickupItem != null && interactionEvent.Source is IContainerProvider containerProvider)
            {
                pickupItem.Container.TransferItemToOther(pickupItem, containerProvider.Container);
            }

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => false;

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
