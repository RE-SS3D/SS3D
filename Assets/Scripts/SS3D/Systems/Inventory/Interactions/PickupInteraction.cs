using SS3D.Data;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using SS3D.Data.Generated;
using SS3D.Systems.Combat;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.Inventory.Interactions
{
    // A pickup interaction is when you pick an item and
    // add it into a container (in this case, the hands)
    // you can only pick things that are not in a container
    public class PickupInteraction : IInteraction, IClientInteractionSource
    {
        public string Name;
        public Sprite Icon;

        public int Priority => 30;

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Pick up";
        }

        public string GetGenericName() => "Pickup";

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Take);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            // if the target is whatever the hell Alain did
            // and the part that matters, if the interaction source is a hand
            if (target is IGameObjectProvider targetBehaviour && source is Hand hand)
            {
                // check that the item is within range
                bool isInRange = InteractionExtensions.RangeCheck(interactionEvent);

                if (!isInRange)
                {
                    return false;
                }

                // check that our hand is empty (two-hand rifles may still route to the other hand)
                Hands hands = hand.HandsController;
                if (hands == null)
                {
                    hands = hand.GetComponentInParent<Hands>();
                }

                // try to get the Item component from the GameObject we just interacted with
                // you can only pickup items (for now, TODO: we have to consider people too), which makes sense
                Item item = targetBehaviour.GameObject.GetComponent<Item>();

                if (item == null)
                {
                    return false;
                }

                // check the item is not in a container
                if (item.IsInContainer())
                {
                    return false;
                }

                // Active left + empty right still offers Pick up for an M4 (auto-routes to right).
                return TwoHandedWeaponRules.TryResolveHandForItem(hands, item, hand, out _);
            }

            return false;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            // remember that when we call this Start, we are starting the interaction per se
            // so we check if the source of the interaction is a Hand, and if the target is an Item
            if (interactionEvent.Source is Hand hand && interactionEvent.Target is Item target)
            {
                // and then we run the function that adds it to the container
                // (Hand.Pickup auto-routes two-hand rifles to RequiredHand)
                hand.Pickup(target);
            }

            return false;
        }
    }
}
