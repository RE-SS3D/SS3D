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
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            var target = interactionEvent.Target.GetComponent<ViewableContainer>();
            if (interactionEvent.Source.Parent is Hands hands && target != null)
            {
                return !hands.SelectedHandEmpty && CanStore(interactionEvent.Source.GetComponentInTree<Creature>(), interactionEvent.GetSourceItem(), target);
            }

            return false;
        }

        private bool CanStore(Creature creature, Item item, ViewableContainer target)
        {
            return target.CanModify(creature) && target.AttachedContainer.Container.CouldStoreItem(item);
        }

        public virtual bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hands hands = (Hands) interactionEvent.Source.Parent;
            hands.ItemInHand.Container = interactionEvent.Target.GetComponent<ViewableContainer>().AttachedContainer.Container;

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