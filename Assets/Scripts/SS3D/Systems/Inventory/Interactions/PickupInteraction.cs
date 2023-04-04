using SS3D.Data;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.GameModes.Events;
using UnityEngine;
using SS3D.Data.Enums;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.Inventory.Interactions
{
    // A pickup interaction is when you pick an item and
    // add it into a container (in this case, the hands)
    // you can only pick things that are not in a container
    public class PickupInteraction : Interaction
    {
        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Pick up";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : Assets.Get(InteractionIcons.Take);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            // if the target is whatever the hell Alain did
            // and the part that matters, if the interaction source is a hand
            if (target is IGameObjectProvider targetBehaviour && source is Hands hands)
            {
		        // if the selected hand is not empty we return false
                if (hands.SelectedHandEmpty)
                {
                    return true;
                }

		        // we try to get the Item component from the GameObject we just interacted with
		        // you can only pickup items (for now, TODO: we have to consider people too), which makes sense
                Item item = targetBehaviour.GameObject.GetComponent<Item>();
                if (item == null)
                {
                    return false;
                }

                bool isInRange = InteractionExtensions.RangeCheck(interactionEvent);
                bool notInAContainer = !item.InContainer();
                // then we just do a range check, to make sure we can interact
		        // and we check if the item is not in a container, you can only pick things that are not in a container
                return isInRange && notInAContainer;
            }

            return false;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            // remember that when we call this Start, we are starting the interaction per se
            // so we check if the source of the interaction is a Hand, and if the target is an Item
            if (interactionEvent.Source is Hands hands && interactionEvent.Target is Item target)
            {
                // and then we run the function that adds it to the container
                hands.Pickup(target);


                try {
                    string ckey = hands.Inventory.Body.Mind.Soul.Ckey;

                    // and call the event for picking up items for the Game Mode System
                    new ItemPickedUpEvent(target, ckey).Invoke(this);
                }
                catch { Debug.Log("Couldn't get Player Ckey"); }
            }

            return false;
        }
    }
}
