using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Equip / fire / stance rules for firearms that occupy both hands (M4: right-only, left reserved).
    /// </summary>
    public static class TwoHandedWeaponRules
    {
        public static bool RequiresBothHands(Item item, out HandSide requiredHand)
        {
            requiredHand = HandSide.Right;
            if (item == null || !item.TryGetComponent(out RangedWeaponItemExtension weapon))
            {
                return false;
            }

            if (!weapon.Profile.RequiresBothHands)
            {
                return false;
            }

            requiredHand = weapon.Profile.RequiredHand;
            return true;
        }

        /// <summary>
        /// Whether <paramref name="item"/> may enter <paramref name="targetHand"/>.
        /// Two-hand weapons must land in <see cref="RangedWeaponProfile.RequiredHand"/> with the
        /// off-hand empty; a reserved off-hand rejects any new item while the rifle is wielded.
        /// </summary>
        public static bool CanPlaceInHand(Item item, Hand targetHand, Hands hands)
        {
            if (item == null || targetHand == null)
            {
                return true;
            }

            if (RequiresBothHands(item, out HandSide requiredHand))
            {
                if (targetHand.Side != requiredHand)
                {
                    return false;
                }

                Hand offHand = FindOtherHand(hands, targetHand);
                if (offHand != null && !offHand.IsEmpty())
                {
                    return false;
                }
            }

            return !IsHandReserved(targetHand, hands);
        }

        /// <summary>
        /// Picks the hand that should receive <paramref name="item"/>. Two-hand firearms route to
        /// <see cref="RangedWeaponProfile.RequiredHand"/> even when the other hand is active.
        /// </summary>
        public static bool TryResolveHandForItem(Hands hands, Item item, Hand preferredHand, out Hand targetHand)
        {
            targetHand = null;
            if (item == null || hands == null)
            {
                return false;
            }

            if (RequiresBothHands(item, out HandSide requiredSide))
            {
                Hand required = FindHand(hands, requiredSide);
                if (required == null || !CanPlaceInHand(item, required, hands) || !required.IsEmpty())
                {
                    return false;
                }

                targetHand = required;
                return true;
            }

            if (preferredHand == null || !preferredHand.IsEmpty() || !CanPlaceInHand(item, preferredHand, hands))
            {
                return false;
            }

            targetHand = preferredHand;
            return true;
        }

        /// <summary>
        /// When dropping onto a hand well, retarget two-hand firearms from the wrong/reserved hand
        /// to the required grip hand so HUD highlight and transfer match pickup auto-route.
        /// </summary>
        public static bool TryResolveHandContainerForItem(
            Hands hands,
            Item item,
            AttachedContainer preferredContainer,
            out AttachedContainer targetContainer)
        {
            targetContainer = preferredContainer;
            if (hands == null || item == null || preferredContainer == null)
            {
                return preferredContainer != null;
            }

            Hand preferredHand = FindHandForContainer(hands, preferredContainer);
            if (!TryResolveHandForItem(hands, item, preferredHand, out Hand targetHand) || targetHand?.Container == null)
            {
                return false;
            }

            targetContainer = targetHand.Container;
            return true;
        }

        public static Hand FindHand(Hands hands, HandSide side)
        {
            if (hands?.PlayerHands == null)
            {
                return null;
            }

            foreach (Hand hand in hands.PlayerHands)
            {
                if (hand != null && hand.Side == side)
                {
                    return hand;
                }
            }

            return null;
        }

        public static Hand FindHandForContainer(Hands hands, AttachedContainer container)
        {
            if (hands?.PlayerHands == null || container == null)
            {
                return null;
            }

            foreach (Hand hand in hands.PlayerHands)
            {
                if (hand != null && hand.Container == container)
                {
                    return hand;
                }
            }

            return null;
        }

        /// <summary>
        /// True when the other hand holds a two-hand weapon in its required hand — this hand is blocked.
        /// </summary>
        public static bool IsHandReserved(Hand hand, Hands hands)
        {
            Hand other = FindOtherHand(hands, hand);
            if (other == null)
            {
                return false;
            }

            Item otherItem = other.ItemInHand;
            if (!RequiresBothHands(otherItem, out HandSide requiredHand))
            {
                return false;
            }

            return other.Side == requiredHand;
        }

        /// <summary>
        /// Held firearm for fire/reload. Two-hand rifles resolve from their required hand even when
        /// the other hand is selected for UI; one-hand guns use the selected hand only.
        /// </summary>
        public static bool TryGetWieldedRangedWeapon(
            Hands hands,
            out Hand holdingHand,
            out RangedWeaponItemExtension weapon)
        {
            holdingHand = null;
            weapon = null;
            if (hands?.PlayerHands == null)
            {
                return false;
            }

            foreach (Hand hand in hands.PlayerHands)
            {
                if (hand == null)
                {
                    continue;
                }

                Item item = hand.ItemInHand;
                if (item == null || !item.TryGetComponent(out RangedWeaponItemExtension candidate))
                {
                    continue;
                }

                if (!candidate.Profile.RequiresBothHands)
                {
                    continue;
                }

                if (hand.Side != candidate.Profile.RequiredHand)
                {
                    continue;
                }

                holdingHand = hand;
                weapon = candidate;
                return true;
            }

            Hand selected = hands.SelectedHand;
            Item selectedItem = selected?.ItemInHand;
            if (selectedItem != null && selectedItem.TryGetComponent(out weapon))
            {
                if (weapon.Profile.RequiresBothHands && selected.Side != weapon.Profile.RequiredHand)
                {
                    weapon = null;
                    return false;
                }

                holdingHand = selected;
                return true;
            }

            weapon = null;
            return false;
        }

        /// <summary>Item that drives combat stance / arm-hold while a two-hand rifle is wielded.</summary>
        public static Item ResolvePresentationItem(Hands hands)
        {
            if (TryGetWieldedRangedWeapon(hands, out Hand holdingHand, out _))
            {
                return holdingHand.ItemInHand;
            }

            return hands?.SelectedHand?.ItemInHand;
        }

        /// <summary>Two-hand rifles stay right-posed; otherwise mirror follows the selected hand.</summary>
        public static bool ShouldMirrorUpperBody(Hands hands)
        {
            if (TryGetWieldedRangedWeapon(hands, out _, out RangedWeaponItemExtension weapon)
                && weapon.Profile.RequiresBothHands)
            {
                return false;
            }

            Hand active = hands?.SelectedHand;
            return active != null && active.Side == HandSide.Left;
        }

        /// <summary>
        /// True when the wielded gun uses <see cref="WeaponHoldStyle.Pistol"/>. <paramref name="offHandBusy"/>
        /// is true when the other hand holds an item, so that arm keeps its own hold pose instead of the grip.
        /// </summary>
        public static bool IsPistolHold(Hands hands, out bool offHandBusy)
        {
            offHandBusy = false;
            if (!TryGetWieldedRangedWeapon(hands, out Hand holdingHand, out RangedWeaponItemExtension weapon)
                || weapon.Profile.HoldStyle != WeaponHoldStyle.Pistol)
            {
                return false;
            }

            Hand offHand = FindOtherHand(hands, holdingHand);
            offHandBusy = offHand != null && !offHand.IsEmpty();
            return true;
        }

        private static Hand FindOtherHand(Hands hands, Hand hand)
        {
            if (hands?.PlayerHands == null || hand == null)
            {
                return null;
            }

            foreach (Hand candidate in hands.PlayerHands)
            {
                if (candidate != null && candidate != hand)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
