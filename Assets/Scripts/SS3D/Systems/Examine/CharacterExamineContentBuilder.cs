using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Items.Generic;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Resolved icon/name for one paperdoll slot. Both fields are null for an empty slot.
    /// </summary>
    public readonly struct CharacterExamineSlotContent
    {
        public CharacterExamineSlot Slot { get; }

        public Sprite ItemIcon { get; }

        public string ItemName { get; }

        public CharacterExamineSlotContent(CharacterExamineSlot slot, Sprite itemIcon, string itemName)
        {
            Slot = slot;
            ItemIcon = itemIcon;
            ItemName = itemName;
        }
    }

    /// <summary>
    /// Builds the character-examine paperdoll's slot contents from a (possibly remote) character's
    /// <see cref="HumanInventory"/> — read-only, works for any character, not just the local player
    /// (Documents/architecture/systems/examine.md § character examine).
    /// </summary>
    public static class CharacterExamineContentBuilder
    {
        private static readonly CharacterExamineSlot[] ContainerBackedSlots =
        {
            CharacterExamineSlot.Head,
            CharacterExamineSlot.Eyes,
            CharacterExamineSlot.Face,
            CharacterExamineSlot.Ears,
            CharacterExamineSlot.Suit,
            CharacterExamineSlot.Shirt,
            CharacterExamineSlot.Gloves,
            CharacterExamineSlot.Back,
            CharacterExamineSlot.Feet,
            CharacterExamineSlot.Belt,
            CharacterExamineSlot.IdCard,
            CharacterExamineSlot.Pocket,
            CharacterExamineSlot.SuitStorage,
        };

        /// <summary>
        /// Builds all 15 paperdoll slots (13 container-backed + 2 hands). Covered/obscured-slot
        /// filtering is not implemented yet — no existing item/clothing data tracks "visually hidden by
        /// an outer layer", so every equipped item currently shows. See examine.md fork-deviations.
        /// </summary>
        public static IReadOnlyList<CharacterExamineSlotContent> BuildSlots(HumanInventory inventory)
        {
            List<CharacterExamineSlotContent> slots = new(14);

            foreach (CharacterExamineSlot slot in ContainerBackedSlots)
            {
                slots.Add(BuildContainerSlot(inventory, slot));
            }

            slots.Add(BuildHandSlot(inventory, CharacterExamineSlot.HandLeft, 0));
            slots.Add(BuildHandSlot(inventory, CharacterExamineSlot.HandRight, 1));
            return slots;
        }

        /// <summary>
        /// Name/job for the examine window title, per examine.md §7 and id-access.md §3: visible only
        /// when an ID is worn in the gear-strip Identification slot — not merely carried in a PDA.
        /// </summary>
        public static bool TryGetVisibleIdentity(HumanInventory inventory, out string name, out string job)
        {
            name = null;
            job = null;

            if (inventory == null
                || !inventory.TryGetTypeContainer(ContainerType.Identification, 0, out AttachedContainer container))
            {
                return false;
            }

            if (container.Items.FirstOrDefault() is not IDCard idCard || string.IsNullOrWhiteSpace(idCard.OwnerName))
            {
                return false;
            }

            name = idCard.OwnerName;
            job = idCard.RoleName;
            return true;
        }

        /// <summary>
        /// Resolves the item currently shown in a paperdoll slot (container-backed or hand).
        /// </summary>
        public static bool TryGetItemInSlot(HumanInventory inventory, CharacterExamineSlot slot, out Item item)
        {
            item = null;
            if (inventory == null)
            {
                return false;
            }

            if (slot == CharacterExamineSlot.HandLeft)
            {
                item = ItemInHand(inventory, 0);
                return item != null;
            }

            if (slot == CharacterExamineSlot.HandRight)
            {
                item = ItemInHand(inventory, 1);
                return item != null;
            }

            if (!CharacterExamineSlotContainerMap.TryGetContainerTypes(slot, out ContainerType primary, out ContainerType secondary))
            {
                return false;
            }

            item = ItemIn(inventory, primary);
            if (item == null && secondary != ContainerType.None)
            {
                item = ItemIn(inventory, secondary);
            }

            return item != null;
        }

        private static CharacterExamineSlotContent BuildContainerSlot(HumanInventory inventory, CharacterExamineSlot slot)
        {
            if (!TryGetItemInSlot(inventory, slot, out Item item))
            {
                return new CharacterExamineSlotContent(slot, null, null);
            }

            return new CharacterExamineSlotContent(slot, item.GetHudSprite(preferWornShape: true), item.Name);
        }

        private static CharacterExamineSlotContent BuildHandSlot(HumanInventory inventory, CharacterExamineSlot slot, int handIndex)
        {
            Item item = ItemInHand(inventory, handIndex);
            return new CharacterExamineSlotContent(slot, item?.ItemSprite, item?.Name);
        }

        private static Item ItemInHand(HumanInventory inventory, int handIndex)
        {
            Hand hand = inventory?.Hands != null && handIndex < inventory.Hands.PlayerHands.Count
                ? inventory.Hands.PlayerHands[handIndex]
                : null;
            return hand?.ItemInHand;
        }

        private static Item ItemIn(HumanInventory inventory, ContainerType type)
        {
            return inventory.TryGetTypeContainer(type, 0, out AttachedContainer container)
                ? container.Items.FirstOrDefault()
                : null;
        }
    }
}

