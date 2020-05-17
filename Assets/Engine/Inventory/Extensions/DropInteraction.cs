using SS3D.Engine.Interactions;

namespace SS3D.Engine.Inventory.Extensions
{
    public class DropInteraction : IInteraction
    {
        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Drop";
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            return interactionEvent.Source.Parent is Hands;
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