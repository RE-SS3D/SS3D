using SS3D.Content.Furniture;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public class StoreInteraction : IInteraction
    {
        public Sprite icon;
        private ContainerDescriptor containerDescriptor;

        public StoreInteraction(ContainerDescriptor containerDescriptor)
        {
            this.containerDescriptor = containerDescriptor;
        }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public virtual string GetName(InteractionEvent interactionEvent)
        {
            return "Store in " + containerDescriptor.containerName;
        }

        public virtual Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return icon;
        }

        public virtual bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            var target = containerDescriptor.attachedContainer;
            if (interactionEvent.Source.Parent is Hands hands && target != null)
            {
                return !hands.SelectedHandEmpty && CanStore(interactionEvent.GetSourceItem(), target);
            }
            return false;
        }

        private bool CanStore(Item item, AttachedContainer target)
        {
            Container container = target.Container;
            return container.CouldStoreItem(item) && container.CouldHoldItem(item);
        }

        public virtual bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hands hands = (Hands) interactionEvent.Source.Parent;
            containerDescriptor.attachedContainer.Container.AddItem(hands.ItemInHand);

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