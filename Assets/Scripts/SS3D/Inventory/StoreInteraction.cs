using SS3D.Engine.Inventory;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Inventory.Extensions;
using UnityEngine;

namespace SS3D.Inventory
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
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Store in " + _containerDescriptor.containerName;
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            AttachedContainer target = _containerDescriptor.attachedContainer;
            if (interactionEvent.Source is Hands hands && target != null)
            {
                return !hands.SelectedHandEmpty && CanStore(interactionEvent.Source.GetComponent<Item>(), target);
            }
            return false;
        }

        private bool CanStore(Item item, AttachedContainer target)
        {
            Container container = target.Container;
            return container.CouldStoreItem(item) && container.CouldHoldItem(item);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hands hands = interactionEvent.Source.GetComponent<Hands>(); 
            _containerDescriptor.attachedContainer.Container.AddItem(hands.ItemInHand);

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }
    }
}