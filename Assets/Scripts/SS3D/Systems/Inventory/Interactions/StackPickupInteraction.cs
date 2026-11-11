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
    /// <summary>
    /// Picks a loose stack up onto the matching stack in hand (floor tiles onto floor tiles). Pickup needs
    /// an empty hand; this is its counterpart when the hand already holds the same item with room for it.
    /// </summary>
    public sealed class StackPickupInteraction : IInteraction, IClientInteractionSource, IHeldItemInteraction
    {
        public int Priority => 30;

        public string GetName(InteractionEvent interactionEvent) => "Add to stack";

        // Same generic name as Pickup, so it plays the same reach or floor pickup.
        public string GetGenericName() => "Pickup";

        public Sprite GetIcon(InteractionEvent interactionEvent) =>
            Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Take);

        public bool CanInteract(InteractionEvent interactionEvent) =>
            TryResolve(interactionEvent, out _, out _) && InteractionExtensions.RangeCheck(interactionEvent);

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (TryResolve(interactionEvent, out Hand hand, out Item target))
            {
                hand.StackOnto(target);
            }

            return false;
        }

        private static bool TryResolve(InteractionEvent interactionEvent, out Hand hand, out Item target)
        {
            hand = interactionEvent?.Source?.GetRootSource() as Hand;
            target = (interactionEvent?.Target as IGameObjectProvider)?.GameObject?.GetComponent<Item>();
            return interactionEvent?.Source is Item && hand != null && target != null && !target.IsInContainer()
                && hand.CanStackOnto(target);
        }
    }
}
