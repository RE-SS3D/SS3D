using SS3D.Content.Furniture;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public class StoreInteraction : IInteraction
    {
        public Sprite icon;
        public bool OnlyWhenOpen { get; set; }
        
        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public virtual string GetName(InteractionEvent interactionEvent)
        {
            return "Store";
        }

        public virtual Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return icon;
        }

        public virtual bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }
            
            if (interactionEvent.Source.Parent is Hands hands && interactionEvent.Target is IGameObjectProvider target)
            {
                return hands.GetItemInHand() != null && CanStore(target.GameObject);
            }

            return false;
        }

        public bool CanStore(GameObject target)
        {
            if(OnlyWhenOpen)
                return target.GetComponent<NetworkedOpenable>()?.IsOpen() ?? false;
            return true;
        }

        public virtual bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hands hands = (Hands) interactionEvent.Source.Parent;
            hands.GameObject.GetComponent<Inventory>()
                .MoveItem(hands.ContainerObject, hands.HeldSlot, ((IGameObjectProvider)interactionEvent.Target).GameObject);
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