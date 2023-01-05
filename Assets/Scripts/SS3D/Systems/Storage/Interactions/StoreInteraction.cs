using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Storage.Containers;
using SS3D.Systems.Storage.Items;
using UnityEngine;

namespace SS3D.Systems.Storage.Interactions
{
    public sealed class StoreInteraction : IInteraction
    {
        public Sprite Icon;
        private readonly ContainerDescriptor _containerDescriptor;

        public StoreInteraction(ContainerDescriptor containerDescriptor)
        {
            _containerDescriptor = containerDescriptor;
        }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return new ClientDelayedInteraction();
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Store in " + _containerDescriptor.ContainerName;
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : AssetData.Get(InteractionIcons.Discard);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            AttachedContainer target = _containerDescriptor.AttachedContainer;
            if (interactionEvent.Source is Hands hands && target != null)
            {
                return !hands.SelectedHandEmpty && CanStore(interactionEvent.Source.GetComponent<Item>(), target);
            }
            return false;
        }

        private bool CanStore(Item item, AttachedContainer target)
        {
            Container container = target.Container;
            return container.CanStoreItem(item) && container.CanHoldItem(item);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hands hands = interactionEvent.Source.GetComponent<Hands>();
            _containerDescriptor.AttachedContainer.Container.AddItem(hands.ItemInHand);

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