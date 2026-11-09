using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// A timed object step from <see cref="ConstructionRecipes"/>: a held tool or material used on a
    /// placed tile object (pry a floor tile, weld plating). Cancels like every <see cref="DelayedInteraction"/>;
    /// materials are checked at start and spent only when the outcome applies.
    /// </summary>
    /// <remarks>Redeclares <see cref="IInteraction"/> so <see cref="Priority"/> reaches interface dispatch
    /// (structural-destruction Pitfall: defuse lost to pickup without it).</remarks>
    public sealed class ConstructionStepInteraction : DelayedInteraction, IInteraction
    {
        private readonly ConstructionRule _rule;
        private readonly PlacedTileObject _target;

        public ConstructionStepInteraction(ConstructionRule rule, PlacedTileObject target)
        {
            _rule = rule;
            _target = target;
            Delay = rule.Seconds;
            CheckInterval = 0.2f;
        }

        public ConstructionRule Rule => _rule;

        /// <summary>Above pickup, store and open (≤ 30), below defuse (40).</summary>
        public int Priority => 35;

        public override string GetName(InteractionEvent interactionEvent) => _rule.Verb;

        public override string GetGenericName() => _rule.GenericName;

        public override Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIconLookup.Take;

        public override bool CanInteract(InteractionEvent interactionEvent) =>
            _target != null
            && ConstructionObjectRules.Matches(_rule, _target)
            && HoldsInput(interactionEvent, out Item item)
            && HasMaterials(item)
            && InteractionExtensions.RangeCheck(interactionEvent);

        protected override DelayedCancelReason ResolveCannotInteractReason(InteractionEvent interactionEvent)
        {
            if (!HoldsInput(interactionEvent, out Item item) || !HasMaterials(item))
            {
                return DelayedCancelReason.MissingMaterials;
            }

            return DelayedCancelReason.Interrupted;
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (!HoldsInput(interactionEvent, out Item item))
            {
                return;
            }

            Hand hand = interactionEvent.Source.GetRootSource() as Hand;
            ConstructionApply.TryApplyObject(_rule, _target, item, hand);
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference) { }

        private bool HoldsInput(InteractionEvent interactionEvent, out Item item)
        {
            item = interactionEvent?.Source?.GetComponent<Item>();
            return item != null && ConstructionInputs.TryGet(item, out ConstructionInput input) && input == _rule.Input;
        }

        private bool HasMaterials(Item item) =>
            !ConstructionInputs.IsMaterial(_rule.Input) || item.StackCount >= _rule.Cost;
    }
}
