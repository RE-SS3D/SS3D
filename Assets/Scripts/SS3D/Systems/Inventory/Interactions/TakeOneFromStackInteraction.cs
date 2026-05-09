using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public sealed class TakeOneFromStackInteraction : IInteraction, IClientInteractionSource
    {
        private readonly Stackable _stackable;

        public TakeOneFromStackInteraction(Stackable stackable)
        {
            _stackable = stackable;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Take one";
        }

        public string GetGenericName() => "Take one";

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Take);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (_stackable == null || _stackable.Amount <= 1)
            {
                return false;
            }

            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            return interactionEvent.Source is Hand hand && hand.IsEmpty();
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is not Hand hand || !hand.IsEmpty())
            {
                return false;
            }

            Item item = _stackable.TakeOne();
            if (item != null)
            {
                hand.Pickup(item);
            }

            return false;
        }
    }
}
