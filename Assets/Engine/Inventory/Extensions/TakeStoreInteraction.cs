using SS3D.Content.Furniture;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public class Store : StoreInteraction
    {
        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            if (interactionEvent.Source.Parent is Hands hands && interactionEvent.Target is IGameObjectProvider target)
            {
                Item handItem = hands.GetItemInHand();
                if (handItem != null && CanStore(target.GameObject))
                {
                    return handItem.GetComponent<IChargeable>() != null;
                }
            }

            return false;
        }
    }

    public class TakeStoreInteraction : InteractionTargetNetworkBehaviour
    {
        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = new List<IInteraction>();
            Store storeInteraction = new Store { OnlyWhenOpen = false };
            var takeInteraction = new SimpleInteraction
            {
                Name = "Take",
                CanInteractCallback = CanTake,
                Interact = Take
            };

            interactions.Insert(0, storeInteraction);
            interactions.Insert(1, takeInteraction);

            return interactions.ToArray();
        }

        private void Take(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            Hands hands = (Hands)interactionEvent.Source.GetRootSource();
            hands.GameObject.GetComponent<Inventory>()
                            .MoveItem(((IGameObjectProvider)interactionEvent.Target).GameObject, 0, hands.ContainerObject, hands.HeldSlot);
        }

        private bool CanTake(InteractionEvent interactionEvent)
        {
            Hands hands = (Hands)interactionEvent.Source.GetRootSource();
            return (((IGameObjectProvider)interactionEvent.Target).GameObject.GetComponent<Container>().GetItem(0) != null);
        }
    }
}
