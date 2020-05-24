using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using UnityEngine;

namespace SS3D.Engine.Inventory.Extensions
{
    public class PickupInteraction : IInteraction
    {

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Pick up";
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is IGameObjectProvider targetBehaviour)
            {
                if (targetBehaviour.GameObject.GetComponent<Item>() == null)
                {
                    return false;
                }

                return InteractionHelpers.RangeCheck(interactionEvent);
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