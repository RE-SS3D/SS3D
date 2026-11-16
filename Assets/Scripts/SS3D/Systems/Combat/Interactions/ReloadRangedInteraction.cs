using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Combat;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Combat.Interactions
{
    /// <summary>
    /// Timed magazine refill for a held ranged weapon (no loose ammo items this pass).
    /// Discovered as source-only when the gun is the active interaction source, or started via
    /// <see cref="InteractionController"/> Use / empty-mag fire.
    /// </summary>
    public sealed class ReloadRangedInteraction : IInteraction, IClientInteractionSource
    {
        private readonly RangedWeaponItemExtension _weapon;

        public ReloadRangedInteraction(RangedWeaponItemExtension weapon)
        {
            _weapon = weapon;
        }

        public string GetName(InteractionEvent interactionEvent) => "Reload";

        public string GetGenericName() => "Reload";

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Take);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (_weapon == null || !_weapon.CanStartReload())
            {
                return false;
            }

            Hand hand = ResolveHand(interactionEvent?.Source);
            return hand != null && hand.ItemInHand != null
                && hand.ItemInHand.TryGetComponent(out RangedWeaponItemExtension held)
                && held == _weapon;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (_weapon == null || !_weapon.ServerTryBeginReload())
            {
                return false;
            }

            Hand hand = ResolveHand(interactionEvent.Source);
            CombatInteractionNetwork combat = hand != null
                ? hand.GetComponentInParent<CombatInteractionNetwork>()
                : null;
            combat?.ServerNotifyRangedReloadStarted(_weapon);
            return false;
        }

        private static Hand ResolveHand(IInteractionSource source)
        {
            if (source == null)
            {
                return null;
            }

            if (source.GetRootSource() is Hand rootHand)
            {
                return rootHand;
            }

            return source.GetComponentInTree<Hand>();
        }
    }
}
