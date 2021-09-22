using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using System;
using UnityEngine;

namespace SS3D.Engine.Inventory.Extensions
{
    // A pickup interaction is when you pick an item and
    // add it into a container (in this case, the hands)
    // you can only pick things that are not in a container
    public class PickupInteraction : IInteraction
    {
        public Sprite icon;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Pick up";
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return icon;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            // if the target is whatever the hell Alain did
            // and the part that matters, if the interaction source is a hand
            if (interactionEvent.Target is IGameObjectProvider targetBehaviour && interactionEvent.Source is Hands hands)
            {
		        // if the selected hand is not empty we return false
                if (!hands.SelectedHandEmpty)
                {
                    return false;
                }
                
		        // we try to get the Item component from the GameObject we just interacted with
		        // you can only pickup items (for now, TODO: we have to consider people too), which makes sense
                IContainerizable item = targetBehaviour.GameObject.GetComponent<IContainerizable>(); 
                if (item == null)
                {
                    return false;
                }
		        // then we just do a range check, to make sure we can interact
		        // and we check if the item is not in a container, you can only pick things that are not in a container
                return InteractionExtensions.RangeCheck(interactionEvent) && !item.InContainer();
            }

            return false;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
	    // remember that when we call this Start, we are starting the interaction per se
	    // so we check if the source of the interaction is a Hand, and if the target is an Item
            if (interactionEvent.Source is Hands hands && interactionEvent.Target is IContainerizable target)
            {         
		// and then we run the function that adds it to the container
                hands.Pickup(target);
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
