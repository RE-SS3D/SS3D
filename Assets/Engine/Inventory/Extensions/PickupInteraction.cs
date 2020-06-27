using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using System;
using UnityEngine;

namespace SS3D.Engine.Inventory.Extensions
{
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
            if (interactionEvent.Target is IGameObjectProvider targetBehaviour && interactionEvent.Source is Hands hands)
            {
                if (hands.GetItemInHand() != null)
                {
                    return false;
                }
                
                Item item = targetBehaviour.GameObject.GetComponent<Item>();
                if (item == null)
                {
                    return false;
                }

                return InteractionExtensions.RangeCheck(interactionEvent) && !item.InContainer();
            }

            return false;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is Hands hands && interactionEvent.Target is IGameObjectProvider target)
            {
                hands.Pickup(target.GameObject);
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