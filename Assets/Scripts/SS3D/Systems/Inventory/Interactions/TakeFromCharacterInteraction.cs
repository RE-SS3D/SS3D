using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    /// <summary>
    /// Delayed take of one worn/held item from another character into the active hand.
    /// Started from a click on the character-examine paperdoll (not Discover) — see examine click-to-take.
    /// </summary>
    public sealed class TakeFromCharacterInteraction : DelayedInteraction
    {
        public const float DefaultDelaySeconds = 1.5f;
        private const float DefaultCheckIntervalSeconds = 0.25f;

        private readonly HumanInventory _victimInventory;
        private readonly CharacterExamineSlot _slot;
        private readonly Item _item;

        public TakeFromCharacterInteraction(
            HumanInventory victimInventory,
            CharacterExamineSlot slot,
            Item item,
            float delaySeconds = DefaultDelaySeconds)
        {
            _victimInventory = victimInventory;
            _slot = slot;
            _item = item;
            Delay = delaySeconds;
            CheckInterval = DefaultCheckIntervalSeconds;
        }

        public CharacterExamineSlot Slot => _slot;

        public float DelaySeconds => Delay;

        /// <inheritdoc />
        /// UITK slot spinner owns client feedback — no progress ring.
        public override IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        public override string GetName(InteractionEvent interactionEvent) => "Take";

        public override string GetGenericName() => "TakeFromCharacter";

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Source is not Hand hand || !hand.IsEmpty())
            {
                return false;
            }

            if (_victimInventory == null || _item == null)
            {
                return false;
            }

            HumanInventory takerInventory = hand.GetComponentInParent<HumanInventory>();
            if (!CharacterLootUtility.IsOtherCharacter(takerInventory, _victimInventory))
            {
                return false;
            }

            if (!CharacterLootUtility.IsLootable(_victimInventory))
            {
                return false;
            }

            if (!CharacterExamineContentBuilder.TryGetItemInSlot(_victimInventory, _slot, out Item current)
                || current != _item)
            {
                return false;
            }

            // Hand range against the victim root (paperdoll take has no Discover hit point).
            if (hand.CanInteract(_victimInventory.gameObject))
            {
                return true;
            }

            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is not Hand hand || !hand.IsEmpty())
            {
                return;
            }

            if (!CanInteract(interactionEvent))
            {
                return;
            }

            hand.Pickup(_item);
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
