using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using UnityEngine;

namespace SS3D.Engine.Inventory.Extensions
{
    public class DropInteraction : MonoBehaviour, IInteraction
    {

        public static Sprite icon;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Drop";
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return icon;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!(interactionEvent.Source.Parent is Hands))
            {
                return false;
            }

            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source.Parent is Hands hands)
            {
                hands.PlaceHeldItem(interactionEvent.Point);
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