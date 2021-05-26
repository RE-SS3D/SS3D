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
            Debug.Log("1");
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                Debug.Log("2");
                return false;
            }

            Debug.Log("3");
            var target = interactionEvent.Target.GetComponent<VisibleContainer>();
            if (interactionEvent.Source.Parent is Hands hands && target != null)
            {
                Debug.Log("4");
                return !hands.SelectedHandEmpty && CanStore(interactionEvent.GetSourceItem(), target);
            }

            Debug.Log("5");
            return false;
        }

        private bool CanStore(Item item, VisibleContainer target)
        {
            return target.AttachedContainer.Container.CouldStoreItem(item);
        }

        public virtual bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hands hands = (Hands) interactionEvent.Source.Parent;
            interactionEvent.Target.GetComponent<AttachedContainer>().Container.AddItem(hands.ItemInHand);

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