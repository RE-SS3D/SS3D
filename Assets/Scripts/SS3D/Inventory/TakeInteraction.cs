using SS3D.Engine.Inventory;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Inventory.Extensions;
using UnityEngine;

namespace SS3D.Inventory
{
    // This Interaction takes the first available item inside a container
    public sealed class TakeInteraction : IInteraction
    {
        public Sprite Icon;
        private readonly ContainerDescriptor _containerDescriptor;

        public TakeInteraction(ContainerDescriptor containerDescriptor)
        {
            _containerDescriptor = containerDescriptor;
        }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Take in " + _containerDescriptor.containerName;
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            if(Icon != null)
            {
                Debug.Log("get icon" + Icon.name);
            }
            else
            {
                Debug.Log("icon is null");
            }
            
            return Icon;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            // Will only appear if the current hand is empty and the container isn't empty
            AttachedContainer target = _containerDescriptor.attachedContainer;
            if (interactionEvent.Source is Hands hands && target != null)
            {
                return hands.SelectedHandEmpty && !target.Container.Empty;
            }

            return false;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hands hands = (Hands) interactionEvent.Source;
            int index = _containerDescriptor.attachedContainer.Container.StoredItems.Count - 1;
            Item pickupItem = _containerDescriptor.attachedContainer.Container.StoredItems[index].Item;
            if (pickupItem != null)
            {
                hands.Pickup(pickupItem);
            }
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