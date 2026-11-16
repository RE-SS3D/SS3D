using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Furniture;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Furniture.Disposal
{
    /// <summary>
    /// Drops the held item into a disposal chute (design doc §2) — the same Tier 3 combine grammar
    /// used elsewhere for item-on-object actions (`crafting.md` §2). First live consumer of
    /// <see cref="InteractionTier.Combine"/> in the codebase.
    /// </summary>
    public sealed class DisposalDropInInteraction : IInteraction, IInteractionTierProvider, ITargetedInteraction
    {
        private readonly DisposalBin _bin;

        public DisposalDropInInteraction(DisposalBin bin)
        {
            _bin = bin;
        }

        public string GetName(InteractionEvent interactionEvent) => "Dispose";

        public string GetGenericName() => "Dispose";

        public Sprite GetIcon(InteractionEvent interactionEvent) =>
            Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Dispose);

        /// <summary>
        /// Above <see cref="SS3D.Systems.Inventory.Interactions.DropInteraction"/> (5) so primary-click
        /// on a chute while holding an item prefers Dispose over Drop.
        /// </summary>
        public int Priority => 40;

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Combine;

        public bool CanTarget(InteractionEvent originEvent, InteractionEvent targetEvent)
        {
            InteractionEvent combined = targetEvent.WithSource(originEvent.Source);
            return CanInteract(combined);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            if (interactionEvent.Source is not IGameObjectProvider sourceProvider)
            {
                return false;
            }

            Hands hands = sourceProvider.GameObject.GetComponentInParent<Hands>();
            if (hands == null || hands.SelectedHand.IsEmpty())
            {
                return false;
            }

            Item item = hands.SelectedHand.ItemInHand;
            if (item == null || !_bin.AcceptsSize(item.SizeClass))
            {
                return false;
            }

            if (_bin.AccessGate != null && !HasRequiredAccess(hands))
            {
                return false;
            }

            return true;
        }

        private bool HasRequiredAccess(Hands hands)
        {
            HumanInventory inventory = hands.GetComponentInParent<HumanInventory>();
            if (inventory == null || !SubSystems.TryGet(out IdAccessSubSystem idAccess))
            {
                return false;
            }

            AccessMask required = _bin.AccessGate.ResolveRequiredAccess();
            if (required.IsNone)
            {
                return true;
            }

            return idAccess.CheckAccess(inventory, required, _bin.AccessGate.AuthLogDevice).Passed;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is not IGameObjectProvider sourceProvider)
            {
                return false;
            }

            Hands hands = sourceProvider.GameObject.GetComponentInParent<Hands>();
            if (hands == null)
            {
                return false;
            }

            Item item = hands.SelectedHand.ItemInHand;
            if (item == null)
            {
                return false;
            }

            Department destination = Department.None;
            if (item.TryGetComponent(out DisposalTag tag))
            {
                destination = tag.Destination;
            }

            hands.SelectedHand.Container.RemoveItem(item);
            if (!_bin.TryEnterDisposalNetwork(item, destination))
            {
                // Enter failed (no pipe / no outlet / no route) — put the item back so Dispose
                // does not silently become a Drop.
                hands.SelectedHand.Container.AddItem(item);
                return false;
            }

            return false;
        }
    }
}
