using SS3D.Engine.Interactions;
using UnityEngine;

namespace SS3D.Engine.Inventory.Extensions
{
    public class PickupInteraction : IInteraction
    {
        public float PickupDistance { get; set; } = 1.5f;

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

                if (interactionEvent.Source is IGameObjectProvider sourceBehaviour &&
                    Vector3.Distance(sourceBehaviour.GameObject.transform.position, targetBehaviour.GameObject.transform.position) >
                    PickupDistance)
                {
                    return false;
                }

                return true;
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